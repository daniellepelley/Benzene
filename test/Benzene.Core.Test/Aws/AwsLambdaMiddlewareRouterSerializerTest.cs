using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Xunit;

namespace Benzene.Test.Aws;

/// <summary>
/// The middleware pipeline resolves middleware fresh on every invocation, so each Lambda invocation
/// constructs a new router instance. A per-instance <see cref="DefaultLambdaJsonSerializer"/> meant a
/// new <c>JsonSerializerOptions</c> - and a full System.Text.Json type-metadata rebuild for the event
/// type - on every single invocation (tens of milliseconds for the large AWS event types, per request).
/// The serializer is therefore shared across instances of a router type; these tests pin that down.
/// </summary>
public class AwsLambdaMiddlewareRouterSerializerTest
{
    private class ExposingRouter : AwsLambdaMiddlewareRouter<string>
    {
        public ExposingRouter() : base(new NullServiceResolver()) { }
        public ILambdaSerializer ExposedSerializer => JsonSerializer;
        protected override bool CanHandle(string request) => false;
        protected override Task HandleFunction(string request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory) => Task.CompletedTask;
    }

    private sealed class OverridingRouter : ExposingRouter
    {
        public OverridingRouter()
        {
            JsonSerializer = new DefaultLambdaJsonSerializer();
        }
    }

    [Fact]
    public void JsonSerializer_IsSharedAcrossRouterInstances()
    {
        Assert.Same(new ExposingRouter().ExposedSerializer, new ExposingRouter().ExposedSerializer);
    }

    [Fact]
    public void JsonSerializer_CanStillBeReplacedPerInstance()
    {
        var overriding = new OverridingRouter();

        Assert.NotSame(new ExposingRouter().ExposedSerializer, overriding.ExposedSerializer);
        // Assigning one instance's field must not affect any other router.
        Assert.Same(new ExposingRouter().ExposedSerializer, new ExposingRouter().ExposedSerializer);
    }
}

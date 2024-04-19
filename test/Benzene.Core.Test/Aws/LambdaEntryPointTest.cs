using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Benzene.Abstractions.DI;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.DirectMessage;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws;

public class LambdaEntryPointTest
{
    [Fact]
    public async Task LambdaEntryPoint()
    {
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()))
            .Use(null, async (x, next) =>
            {
                x.Response = new MemoryStream();
                await next();
            });

        var mockServiceResolverFactory = new Mock<IServiceResolverFactory>();

        mockServiceResolverFactory.Setup(x => x.CreateScope())
            .Returns(ServiceResolverMother.CreateServiceResolver());

        var lambdaEntryPoint = new AwsLambdaEntryPoint(app.Build(), mockServiceResolverFactory.Object);

        var request = new DirectMessageRequest();

        var result = await lambdaEntryPoint.FunctionHandler(AwsEventStreamContextBuilder.ObjectToStream(request), new TestLambdaContext());
        Assert.NotNull(result);
    }
}

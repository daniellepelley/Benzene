using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Sns;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.Sns;

public class SnsBenzeneInvocationExtensionsTest
{
    [Fact]
    public async Task UseBenzeneInvocation_SetsInvocationIdToSnsMessageId()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var builder = new MiddlewarePipelineBuilder<SnsRecordContext>(container);
        builder.UseBenzeneInvocation();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var snsEvent = new SNSEvent
        {
            Records = new System.Collections.Generic.List<SNSEvent.SNSRecord>
            {
                new() { Sns = new SNSEvent.SNSMessage { MessageId = "sns-msg-789" } }
            }
        };
        var context = SnsRecordContext.CreateInstance(snsEvent, snsEvent.Records[0]);

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("sns-msg-789", resolved.InvocationId);
        Assert.Equal("AwsLambda", resolved.Platform);
    }
}

using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Aws.EventBridge;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Aws.S3;

public class S3MessagePipelineTest
{
    private static S3Event CreateRequest()
    {
        return new S3Event
        {
            Records =
            [
                new S3Event.S3EventNotificationRecord
                {
                    EventName = "some-event"
                }
            ]
        };
    }

    [Fact]
    public async Task Send()
    {
        string eventName = null;

        var host = new EntryPointMiddleApplicationBuilder<S3Event, S3RecordContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddS3());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    eventName = context.S3EventNotificationRecord.EventName;
                }))
            .Build(x => new S3Application(x));

        await host.HandleAsync(CreateRequest());

        Assert.Equal("some-event", eventName);
    }
}

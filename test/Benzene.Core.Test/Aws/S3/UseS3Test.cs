using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.S3;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.S3;

public class UseS3Test
{
    private static S3Event CreateS3Event(string eventSource = "aws:s3")
    {
        return new S3Event
        {
            Records =
            [
                new S3Event.S3EventNotificationRecord
                {
                    EventName = "ObjectCreated:Put",
                    EventSource = eventSource
                }
            ]
        };
    }

    [Fact]
    public async Task Send_FromStream_RoutesToS3Pipeline()
    {
        S3RecordContext capturedContext = null;
        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(services));

        app.UseS3(message => message
            .Use(null, (context, next) =>
            {
                capturedContext = context;
                return next();
            })
        );

        var s3Event = CreateS3Event();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(s3Event), new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        Assert.NotNull(capturedContext);
        Assert.Equal("ObjectCreated:Put", capturedContext.S3EventNotificationRecord.EventName);
        Assert.NotNull(capturedContext.S3Event);
        Assert.Single(capturedContext.S3Event.Records);
    }

    [Fact]
    public async Task Send_FromStream_NonS3Event_DoesNotRoute()
    {
        S3RecordContext capturedContext = null;
        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(services));

        app.UseS3(message => message
            .Use(null, (context, next) =>
            {
                capturedContext = context;
                return next();
            })
        );

        var nonS3Event = CreateS3Event(eventSource: "aws:sns");

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(nonS3Event), new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        Assert.Null(capturedContext);
    }
}

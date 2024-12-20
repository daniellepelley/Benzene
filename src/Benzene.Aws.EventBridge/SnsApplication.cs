using System.Linq;
using Amazon.Lambda.S3Events;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.EventBridge;

public class S3Application : MiddlewareMultiApplication<S3Event, S3RecordContext>
{
    public S3Application(IMiddlewarePipeline<S3RecordContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<S3RecordContext>("s3", pipeline),
            @event => @event.Records.Select(record => S3RecordContext.CreateInstance(@event, record)).ToArray())
    { }
}

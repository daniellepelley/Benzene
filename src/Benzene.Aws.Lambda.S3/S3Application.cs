using System.Linq;
using Amazon.Lambda.S3Events;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.S3;

/// <summary>
/// Processes an S3 event notification batch by mapping each record to an <see cref="S3RecordContext"/>
/// and running them all through the middleware pipeline, tagging the transport as <c>"s3"</c> for the duration.
/// </summary>
public class S3Application : MiddlewareMultiApplication<S3Event, S3RecordContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S3Application"/> class.
    /// </summary>
    /// <param name="pipeline">The built S3 middleware pipeline to run each record through.</param>
    public S3Application(IMiddlewarePipeline<S3RecordContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<S3RecordContext>(TransportNames.S3, pipeline),
            @event => @event.Records.Select(record => S3RecordContext.CreateInstance(@event, record)).ToArray())
    { }
}

using System.Linq;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Processes an SNS batch event by mapping each record to an <see cref="SnsRecordContext"/> and running
/// them all through the middleware pipeline, tagging the transport as <c>"sns"</c> for the duration.
/// </summary>
public class SnsApplication : MiddlewareMultiApplication<SNSEvent, SnsRecordContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built SNS middleware pipeline to run each record through.</param>
    public SnsApplication(IMiddlewarePipeline<SnsRecordContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<SnsRecordContext>("sns", pipeline),
            @event => @event.Records.Select(record => SnsRecordContext.CreateInstance(@event, record)).ToArray())
    { }
}

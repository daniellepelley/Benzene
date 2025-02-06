using System.Linq;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Sns;

public class SnsApplication : MiddlewareMultiApplication<SNSEvent, SnsRecordContext>
{
    public SnsApplication(IMiddlewarePipeline<SnsRecordContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<SnsRecordContext>("sns", pipeline),
            @event => @event.Records.Select(record => SnsRecordContext.CreateInstance(@event, record)).ToArray())
    { }
}

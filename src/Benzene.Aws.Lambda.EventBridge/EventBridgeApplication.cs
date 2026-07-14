using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Runs one EventBridge event through the <see cref="EventBridgeContext"/> middleware pipeline —
/// a single-context application (one pipeline invocation per event), since EventBridge invokes a
/// Lambda target with exactly one event, not a batch.
/// </summary>
public class EventBridgeApplication : MiddlewareApplication<EventBridgeEvent, EventBridgeContext>
{
    public EventBridgeApplication(IMiddlewarePipeline<EventBridgeContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<EventBridgeContext>("eventbridge", pipeline),
            @event => new EventBridgeContext(@event))
    { }
}

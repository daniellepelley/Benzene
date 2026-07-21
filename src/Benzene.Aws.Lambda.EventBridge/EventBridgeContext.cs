using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// The pipeline context for one EventBridge event. EventBridge delivers a single event per Lambda
/// invocation, so unlike the SQS/SNS record contexts there is no surrounding batch — one event, one
/// context, one DI scope.
/// </summary>
public class EventBridgeContext : IHasMessageResult
{
    public EventBridgeContext(EventBridgeEvent eventBridgeEvent)
    {
        Event = eventBridgeEvent;
    }

    public EventBridgeEvent Event { get; }

    public IBenzeneResult MessageResult { get; set; }
}

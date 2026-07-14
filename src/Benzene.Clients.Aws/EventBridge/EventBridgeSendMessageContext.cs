using Amazon.EventBridge.Model;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// The send-pipeline context for publishing one event to Amazon EventBridge.
/// </summary>
public class EventBridgeSendMessageContext
{
    public EventBridgeSendMessageContext(PutEventsRequest request)
    {
        Request = request;
    }

    public PutEventsRequest Request { get; }

    public PutEventsResponse Response { get; set; }
}

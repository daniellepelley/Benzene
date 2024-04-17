using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Results;

namespace Benzene.Azure.Core.EventHub;

public class EventHubContext// : IHasMessageResult
{
    private EventHubContext(EventData eventData)
    {
        EventData = eventData;
    }
    public static EventHubContext CreateInstance(EventData eventData)
    {
        return new EventHubContext(eventData);
    }
    
    public EventData EventData { get; }
    // public IMessageResult? MessageResult { get; set; }
}

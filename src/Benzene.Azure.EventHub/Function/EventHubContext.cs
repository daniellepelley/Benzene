using Azure.Messaging.EventHubs;

namespace Benzene.Azure.EventHub.Function;

public class EventHubContext
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
}

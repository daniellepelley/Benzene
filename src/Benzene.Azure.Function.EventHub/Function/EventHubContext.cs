using Azure.Messaging.EventHubs;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Provides the middleware pipeline context for a single event within an Event Hub trigger batch.
/// </summary>
public class EventHubContext
{
    private EventHubContext(EventData eventData)
    {
        EventData = eventData;
    }

    /// <summary>
    /// Creates a new <see cref="EventHubContext"/> for a single event.
    /// </summary>
    /// <param name="eventData">The Event Hub event data.</param>
    /// <returns>The created context.</returns>
    public static EventHubContext CreateInstance(EventData eventData)
    {
        return new EventHubContext(eventData);
    }

    /// <summary>
    /// Gets the Event Hub event data.
    /// </summary>
    public EventData EventData { get; }
}

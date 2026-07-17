using Azure.Messaging.EventHubs;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Provides the middleware pipeline context for a single event received by the self-hosted
/// consumer (<see cref="BenzeneEventHubWorker"/>).
/// </summary>
public class EventHubConsumerContext : IHasMessageResult
{
    private EventHubConsumerContext(EventData eventData)
    {
        EventData = eventData;
    }

    /// <summary>
    /// Creates a new <see cref="EventHubConsumerContext"/> for a received event.
    /// </summary>
    /// <param name="eventData">The Event Hub event data.</param>
    /// <returns>The created context.</returns>
    public static EventHubConsumerContext CreateInstance(EventData eventData)
    {
        return new EventHubConsumerContext(eventData);
    }

    /// <summary>
    /// Gets the Event Hub event data.
    /// </summary>
    public EventData EventData { get; }

    /// <summary>
    /// Gets or sets the result of handling this event. Set by
    /// <see cref="EventHubConsumerMessageHandlerResultSetter"/>. Event Hubs has no per-event
    /// settlement, so an unsuccessful result doesn't affect checkpointing - it's recorded for
    /// middleware/diagnostics only.
    /// </summary>
    public IMessageResult MessageResult { get; set; } = null!;
}

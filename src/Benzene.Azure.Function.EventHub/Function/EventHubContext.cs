using Azure.Messaging.EventHubs;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Provides the middleware pipeline context for a single event within an Event Hub trigger batch.
/// </summary>
public class EventHubContext : IHasMessageResult
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

    /// <summary>
    /// Gets or sets the result of handling this event. The Event Hubs trigger has no per-event
    /// settlement (the host checkpoints the whole batch when the invocation returns successfully and
    /// re-delivers the whole batch when it throws), so this is recorded for middleware/diagnostics
    /// and for <see cref="EventHubOptions.RaiseOnFailureStatus"/> only. Note that this package routes
    /// via <c>UseBenzeneMessage</c> (whose response is suppressed), so nothing populates this in the
    /// default envelope path today - see the package CLAUDE.md "Failure handling" section.
    /// </summary>
    public IMessageResult MessageResult { get; set; } = null!;
}

using System;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Thrown by <see cref="EventGridApplication"/> when <see cref="EventGridOptions.RaiseOnFailureStatus"/>
/// is enabled and a message handler reported an unsuccessful result without itself throwing -
/// escalating the failure into an exception so Event Grid's own retry/dead-letter policy applies the
/// same way it would for an unhandled exception.
/// </summary>
public class EventGridMessageProcessingException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EventGridMessageProcessingException"/> class.</summary>
    /// <param name="eventId">The Event Grid event id the handler reported a failure for.</param>
    public EventGridMessageProcessingException(string eventId)
        : base($"Message handler reported an unsuccessful result for Event Grid event {eventId}.")
    {
        EventId = eventId;
    }

    /// <summary>Gets the Event Grid event id the handler reported a failure for.</summary>
    public string EventId { get; }
}

using System;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Thrown by <see cref="EventBridgeApplication"/> when <see cref="EventBridgeOptions.RaiseOnFailureStatus"/>
/// is enabled and a message handler reported an unsuccessful result without itself throwing -
/// escalating the failure into an exception so the EventBridge rule target's own retry/on-failure
/// policy applies the same way it would for an unhandled exception.
/// </summary>
public class EventBridgeMessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeMessageProcessingException"/> class.
    /// </summary>
    /// <param name="eventId">The EventBridge event id the handler reported a failure for.</param>
    public EventBridgeMessageProcessingException(string eventId)
        : base($"Message handler reported an unsuccessful result for EventBridge event {eventId}.")
    {
        EventId = eventId;
    }

    /// <summary>Gets the EventBridge event id the handler reported a failure for.</summary>
    public string EventId { get; }
}

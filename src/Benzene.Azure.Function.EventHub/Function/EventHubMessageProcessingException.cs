using System;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Thrown by <see cref="EventHubBatchApplication"/> when <see cref="EventHubOptions.RaiseOnFailureStatus"/>
/// is enabled and a message handler reported an unsuccessful result without itself throwing -
/// escalating the failure into an exception so the Event Hubs trigger's re-delivery applies the same
/// way it would for an unhandled exception.
/// </summary>
public class EventHubMessageProcessingException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="EventHubMessageProcessingException"/> class.</summary>
    /// <param name="sequenceNumber">The Event Hub event sequence number the handler reported a failure for.</param>
    public EventHubMessageProcessingException(string sequenceNumber)
        : base($"Message handler reported an unsuccessful result for Event Hub event {sequenceNumber}.")
    {
        SequenceNumber = sequenceNumber;
    }

    /// <summary>Gets the Event Hub event sequence number the handler reported a failure for.</summary>
    public string SequenceNumber { get; }
}

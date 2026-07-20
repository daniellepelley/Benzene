using System;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Thrown by <see cref="BenzeneEventHubWorker"/> when
/// <see cref="BenzeneEventHubConfig.RaiseOnFailureStatus"/> is enabled and a handler reported an
/// unsuccessful result without itself throwing - escalating the failure into an exception so it's
/// treated exactly like an unhandled exception (the failed event isn't checkpointed, so the partition
/// doesn't advance past it and a restart redelivers it).
/// </summary>
public class EventHubMessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubMessageProcessingException"/> class.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number of the event the handler reported a failure for.</param>
    /// <param name="partitionId">The partition the failing event was on.</param>
    public EventHubMessageProcessingException(long sequenceNumber, string partitionId)
        : base($"Message handler reported an unsuccessful result for event with sequence number {sequenceNumber} on partition {partitionId}.")
    {
        SequenceNumber = sequenceNumber;
        PartitionId = partitionId;
    }

    /// <summary>Gets the sequence number of the event the handler reported a failure for.</summary>
    public long SequenceNumber { get; }

    /// <summary>Gets the partition the failing event was on.</summary>
    public string PartitionId { get; }
}

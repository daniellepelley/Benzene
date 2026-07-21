namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Configures how <see cref="QueueStorageApplication"/> handles a message handler's exceptions and
/// failure results. Mirrors <c>Benzene.Azure.Function.Kafka</c>'s <c>KafkaOptions</c>.
/// </summary>
public class QueueStorageOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, and the
    /// invocation reports success - so the host deletes the message and its poison protection never
    /// engages) instead of left to cascade and fail the trigger invocation (so the host's
    /// <c>maxDequeueCount</c> poison handling applies). Defaults to <c>false</c> - an exception
    /// usually signals a transient failure worth retrying and eventually poisoning.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result is escalated
    /// into a thrown <see cref="QueueStorageMessageProcessingException"/>, so the failing message is
    /// retried/poisoned by the host the same way it would be for an unhandled exception. Defaults to
    /// <c>true</c> - a returned failure is escalated and redelivered (at-least-once). Set <c>false</c>
    /// for at-most-once (a failure result is accepted, not retried); either way the handler must be idempotent.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of messages from a single batched delivery processed
    /// concurrently. <c>null</c> (the default) leaves the fan-out unbounded. A value &lt;= 0 is
    /// treated the same as <c>null</c>. Has no effect on the default one-message-per-invocation
    /// trigger cardinality.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

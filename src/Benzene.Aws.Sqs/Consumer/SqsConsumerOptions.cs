namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Configures how <see cref="SqsConsumer"/> acknowledges (deletes) messages after processing a poll batch.
/// </summary>
public class SqsConsumerOptions
{
    /// <summary>
    /// Gets or sets whether messages are deleted as a whole batch or individually. Defaults to
    /// <see cref="SqsConsumerAckMode.PerMessage"/> - only messages that actually succeeded are
    /// deleted, so a failed or unrouted message stays on the queue for redelivery/DLQ redrive rather
    /// than being deleted along with the batch. Set <see cref="SqsConsumerAckMode.WholeBatch"/> for
    /// the older all-or-nothing-on-throw behavior.
    /// </summary>
    public SqsConsumerAckMode AckMode { get; set; } = SqsConsumerAckMode.PerMessage;

    /// <summary>
    /// Gets or sets the maximum number of messages from a single poll batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded - every message in the batch starts at
    /// once, the original behavior. Set a positive value to cap concurrency, e.g. to stop a large
    /// batch from opening more scoped database connections than the pool allows. A value &lt;= 0 is
    /// treated the same as <c>null</c> (unbounded).
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

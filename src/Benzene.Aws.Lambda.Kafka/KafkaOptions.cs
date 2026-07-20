namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Configures how <see cref="KafkaApplication"/> handles per-record failures within a Kafka batch.
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Gets or sets how a single record's failure affects the rest of the batch. Defaults to
    /// <see cref="KafkaBatchFailureMode.PartialBatchFailure"/>.
    /// </summary>
    public KafkaBatchFailureMode BatchFailureMode { get; set; } = KafkaBatchFailureMode.PartialBatchFailure;

    /// <summary>
    /// Gets or sets the maximum number of topic-partitions from a single batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded — every partition in the batch starts at
    /// once. Set a positive value to cap concurrency, e.g. to stop a batch spanning many partitions
    /// from opening more scoped database connections than the pool allows. A value &lt;= 0 is treated
    /// the same as <c>null</c> (unbounded).
    /// </summary>
    /// <remarks>
    /// Records <em>within</em> a single topic-partition always run sequentially in offset order (Kafka's
    /// per-partition ordering guarantee); this bound governs how many partitions run in parallel.
    /// </remarks>
    public int? MaxDegreeOfParallelism { get; set; }
}

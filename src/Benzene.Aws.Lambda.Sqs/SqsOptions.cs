namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Configures how <see cref="SqsApplication"/> handles per-message failures within a batch.
/// </summary>
public class SqsOptions
{
    /// <summary>
    /// Gets or sets how a single message's failure affects the rest of the batch. Defaults to
    /// <see cref="SqsBatchFailureMode.PartialBatchFailure"/>.
    /// </summary>
    public SqsBatchFailureMode BatchFailureMode { get; set; } = SqsBatchFailureMode.PartialBatchFailure;

    /// <summary>
    /// Gets or sets the maximum number of records from a single batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded - every record in the batch starts at
    /// once, the original behavior. Set a positive value to cap concurrency, e.g. to stop a large
    /// batch from opening more scoped database connections than the pool allows. A value &lt;= 0 is
    /// treated the same as <c>null</c> (unbounded).
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

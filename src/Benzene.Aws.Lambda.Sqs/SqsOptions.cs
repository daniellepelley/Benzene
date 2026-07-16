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
}

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Controls how a single Kafka record's failure (a thrown exception, or a message handler reporting
/// an unsuccessful result) affects the rest of the batch.
/// </summary>
public enum KafkaBatchFailureMode
{
    /// <summary>
    /// The AWS best-practice default. Each topic-partition that fails is reported back via
    /// <see cref="KafkaBatchResponse"/>'s <c>batchItemFailures</c> naming the offset to resume from,
    /// so the event source mapping redrives just that partition from that offset — the rest of the
    /// batch (and every earlier record in the failed partition) is treated as successfully processed.
    /// Requires <c>ReportBatchItemFailures</c> to be configured on the event source mapping's
    /// <c>FunctionResponseTypes</c>; without it, AWS ignores the returned response and treats the
    /// whole invocation as either fully succeeded or fully failed.
    /// </summary>
    PartialBatchFailure = 0,

    /// <summary>
    /// Any failure in the batch fails the entire Lambda invocation (by throwing), so the event source
    /// mapping retries every record in the batch, not just the partitions that failed. Useful when the
    /// event source mapping doesn't have <c>ReportBatchItemFailures</c> configured, or when a failure
    /// should stop the whole invocation.
    /// </summary>
    FailWholeBatch = 1,
}

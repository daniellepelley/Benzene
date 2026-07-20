using System;
using System.Collections.Generic;
using System.Linq;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Thrown by <see cref="KafkaApplication"/> when <see cref="KafkaOptions.BatchFailureMode"/> is set to
/// <see cref="KafkaBatchFailureMode.FailWholeBatch"/> and at least one record in the batch failed —
/// letting the exception propagate out of the Lambda invocation fails the whole batch, so the event
/// source mapping retries every record rather than just the partitions that actually failed.
/// </summary>
public class KafkaBatchProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaBatchProcessingException"/> class.
    /// </summary>
    /// <param name="failedPartitions">The <c>topic-partition</c> keys that failed within the batch.</param>
    public KafkaBatchProcessingException(IReadOnlyCollection<string> failedPartitions)
        : base($"{failedPartitions.Count} of the batch's topic-partition(s) failed: {string.Join(", ", failedPartitions)}")
    {
        FailedPartitions = failedPartitions;
    }

    /// <summary>
    /// Gets the <c>topic-partition</c> keys that failed within the batch.
    /// </summary>
    public IReadOnlyCollection<string> FailedPartitions { get; }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Thrown by <see cref="SqsApplication"/> when <see cref="SqsOptions.BatchFailureMode"/> is set to
/// <see cref="SqsBatchFailureMode.FailWholeBatch"/> and at least one message in the batch failed -
/// letting the exception propagate out of the Lambda invocation fails the whole batch, so SQS
/// retries/redrives every message rather than just the ones that actually failed.
/// </summary>
public class SqsBatchProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsBatchProcessingException"/> class.
    /// </summary>
    /// <param name="failedMessageIds">The message IDs that failed within the batch.</param>
    public SqsBatchProcessingException(IReadOnlyCollection<string> failedMessageIds)
        : base($"{failedMessageIds.Count} of the batch's message(s) failed: {string.Join(", ", failedMessageIds)}")
    {
        FailedMessageIds = failedMessageIds;
    }

    /// <summary>
    /// Gets the message IDs that failed within the batch.
    /// </summary>
    public IReadOnlyCollection<string> FailedMessageIds { get; }
}

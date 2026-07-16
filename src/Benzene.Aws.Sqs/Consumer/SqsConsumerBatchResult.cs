using System.Collections.Generic;
using Amazon.SQS.Model;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// The outcome of running one poll batch of SQS messages through the middleware pipeline, split into
/// the messages that succeeded and the messages that failed (either a thrown exception or an
/// unsuccessful, non-exception result).
/// </summary>
public class SqsConsumerBatchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsConsumerBatchResult"/> class.
    /// </summary>
    /// <param name="successfulMessages">The messages whose handler completed successfully.</param>
    /// <param name="failedMessages">The messages whose handler threw or returned an unsuccessful result.</param>
    public SqsConsumerBatchResult(IReadOnlyList<Message> successfulMessages, IReadOnlyList<Message> failedMessages)
    {
        SuccessfulMessages = successfulMessages;
        FailedMessages = failedMessages;
    }

    /// <summary>
    /// Gets the messages whose handler completed successfully.
    /// </summary>
    public IReadOnlyList<Message> SuccessfulMessages { get; }

    /// <summary>
    /// Gets the messages whose handler threw or returned an unsuccessful result.
    /// </summary>
    public IReadOnlyList<Message> FailedMessages { get; }
}

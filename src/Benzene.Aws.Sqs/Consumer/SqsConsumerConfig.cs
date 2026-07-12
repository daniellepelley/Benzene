namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Configures the queue and batch size used by <see cref="SqsConsumer"/>.
/// </summary>
public class SqsConsumerConfig
{
    /// <summary>
    /// Gets or sets the URL of the SQS queue to poll.
    /// </summary>
    public string QueueUrl { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages to receive per poll (1-10, per the SQS API).
    /// </summary>
    public int MaxNumberOfMessages { get; set; }

    /// <summary>
    /// Gets or sets the SQS long-poll wait time in seconds (0-20, per the SQS API). Defaults to 1.
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 1;
}

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
    /// Gets or sets the SQS long-poll wait time in seconds (0-20, per the SQS API). Defaults to 20
    /// (maximum long polling), which lets a single receive wait for messages instead of returning
    /// empty immediately - fewer empty receives, so lower request cost and latency. Set a lower value
    /// only if you need the poll loop to spin faster (e.g. to react to shutdown more promptly).
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the message attribute the topic is read from. Defaults to
    /// <see cref="SqsConsumerMessageTopicGetter.DefaultTopicAttribute"/> (<c>"topic"</c>) — set a
    /// different key to consume messages a non-Benzene producer routes on another attribute, without
    /// writing a custom topic getter. Keep it in sync with the producer's attribute key.
    /// </summary>
    public string TopicAttributeKey { get; set; } = SqsConsumerMessageTopicGetter.DefaultTopicAttribute;
}

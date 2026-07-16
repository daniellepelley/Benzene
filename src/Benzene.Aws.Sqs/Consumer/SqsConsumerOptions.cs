namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Configures how <see cref="SqsConsumer"/> acknowledges (deletes) messages after processing a poll batch.
/// </summary>
public class SqsConsumerOptions
{
    /// <summary>
    /// Gets or sets whether messages are deleted as a whole batch or individually. Defaults to
    /// <see cref="SqsConsumerAckMode.WholeBatch"/>.
    /// </summary>
    public SqsConsumerAckMode AckMode { get; set; } = SqsConsumerAckMode.WholeBatch;
}

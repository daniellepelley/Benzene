using Amazon.SQS.Model;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Provides the middleware pipeline context for a single SQS message received by the polling consumer.
/// </summary>
public class SqsConsumerMessageContext
{
    private SqsConsumerMessageContext(Message message)
    {
        Message = message;
    }

    /// <summary>
    /// Creates a new <see cref="SqsConsumerMessageContext"/> for a received SQS message.
    /// </summary>
    /// <param name="message">The received SQS message.</param>
    /// <returns>The created context.</returns>
    public static SqsConsumerMessageContext CreateInstance(Message message)
    {
        return new SqsConsumerMessageContext(message);
    }

    /// <summary>
    /// Gets the received SQS message.
    /// </summary>
    public Message Message { get; }
}

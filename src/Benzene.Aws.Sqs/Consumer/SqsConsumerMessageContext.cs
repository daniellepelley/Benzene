using Amazon.SQS.Model;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Provides the middleware pipeline context for a single SQS message received by the polling consumer.
/// </summary>
public class SqsConsumerMessageContext : IHasMessageResult
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

    /// <summary>
    /// Gets or sets the result of handling this message. Set by
    /// <see cref="SqsConsumerMessageMessageHandlerResultSetter"/>; read by <see cref="SqsConsumerApplication"/>
    /// to support <see cref="SqsConsumerAckMode.PerMessage"/>.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}

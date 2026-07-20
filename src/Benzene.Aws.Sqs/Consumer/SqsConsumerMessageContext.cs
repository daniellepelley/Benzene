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
    /// Gets the number of times this message has been received (SQS's <c>ApproximateReceiveCount</c>
    /// system attribute), or <c>null</c> if it wasn't requested/present. A handler can use it to make
    /// poison-message decisions (e.g. route to a dead-letter path after N deliveries). Requested by
    /// <see cref="SqsConsumer"/> on each receive.
    /// </summary>
    public int? ApproximateReceiveCount =>
        Message?.Attributes != null &&
        Message.Attributes.TryGetValue("ApproximateReceiveCount", out var value) &&
        int.TryParse(value, out var count)
            ? count
            : null;

    /// <summary>
    /// Gets or sets the result of handling this message. Set by
    /// <see cref="SqsConsumerMessageHandlerResultSetter"/>; read by <see cref="SqsConsumerApplication"/>
    /// to support <see cref="SqsConsumerAckMode.PerMessage"/>.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}

namespace Benzene.RabbitMq;

/// <summary>
/// Controls how <see cref="RabbitMqWorker"/> settles each delivery with the broker.
/// </summary>
public enum RabbitMqAckMode
{
    /// <summary>
    /// The worker acknowledges each delivery from the handler's outcome: <c>BasicAck</c> on success,
    /// <c>BasicNack</c> on a thrown exception or an unsuccessful <c>IMessageResult</c>. Whether a
    /// nacked delivery is requeued or routed to a dead-letter exchange is governed by
    /// <see cref="RabbitMqConfig.RequeueOnFailure"/>. This is the default and the safe choice - a
    /// failed message is redelivered or dead-lettered rather than silently lost.
    /// </summary>
    Explicit,

    /// <summary>
    /// The broker auto-acknowledges each delivery the moment it is dispatched to the consumer
    /// (<c>autoAck: true</c>), before the handler runs. Lowest overhead, but a handler failure - or a
    /// worker crash mid-handling - loses the message with no redelivery. Only for at-most-once,
    /// loss-tolerant workloads.
    /// </summary>
    AutoAck,
}

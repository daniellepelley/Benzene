using Benzene.Abstractions.MessageHandlers;
using RabbitMQ.Client.Events;

namespace Benzene.RabbitMq.RabbitMqMessage;

/// <summary>
/// The middleware pipeline context for a single RabbitMQ delivery consumed by
/// <see cref="RabbitMqWorker"/>. Wraps the raw <see cref="BasicDeliverEventArgs"/> (body, headers,
/// routing key, delivery tag) - transport shape only (context purity) - and carries the handler's
/// outcome on <see cref="MessageResult"/>, which the worker reads to decide the per-message
/// ack/nack under <see cref="RabbitMqAckMode.Explicit"/>.
/// </summary>
public class RabbitMqContext : IHasMessageResult
{
    private RabbitMqContext(BasicDeliverEventArgs deliverEventArgs)
    {
        DeliverEventArgs = deliverEventArgs;
    }

    /// <summary>Creates a new <see cref="RabbitMqContext"/> for a received delivery.</summary>
    /// <param name="deliverEventArgs">The raw RabbitMQ delivery.</param>
    /// <returns>The created context.</returns>
    public static RabbitMqContext CreateInstance(BasicDeliverEventArgs deliverEventArgs)
    {
        return new RabbitMqContext(deliverEventArgs);
    }

    /// <summary>Gets the raw RabbitMQ delivery this context wraps.</summary>
    public BasicDeliverEventArgs DeliverEventArgs { get; }

    /// <summary>
    /// Gets or sets the result of handling this delivery. Set by
    /// <see cref="RabbitMqMessageHandlerResultSetter"/>; read by <see cref="RabbitMqWorker"/> to
    /// decide ack vs nack under <see cref="RabbitMqAckMode.Explicit"/>.
    /// </summary>
    public IMessageResult MessageResult { get; set; } = null!;
}

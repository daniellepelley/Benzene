using Benzene.Abstractions.Middleware;
using RabbitMQ.Client;

namespace Benzene.RabbitMq.RabbitMqSendMessage;

/// <summary>
/// The transport middleware at the bottom of an outbound RabbitMQ pipeline: publishes the
/// <see cref="RabbitMqSendMessageContext"/> to its exchange/routing key via the shared
/// <see cref="IChannel"/>, forwarding the Benzene headers onto <c>BasicProperties</c>.
/// </summary>
public class RabbitMqClientMiddleware : IMiddleware<RabbitMqSendMessageContext>
{
    private readonly IChannel _channel;
    private readonly bool _mandatory;

    /// <summary>Initializes a new instance of the <see cref="RabbitMqClientMiddleware"/> class.</summary>
    /// <param name="channel">The RabbitMQ channel to publish on.</param>
    /// <param name="mandatory">
    /// When <c>true</c>, an unroutable message (no queue bound for the routing key) is returned by the
    /// broker rather than silently dropped. Defaults to <c>false</c>.
    /// </param>
    public RabbitMqClientMiddleware(IChannel channel, bool mandatory = false)
    {
        _channel = channel;
        _mandatory = mandatory;
    }

    /// <inheritdoc />
    public string Name => nameof(RabbitMqClientMiddleware);

    /// <inheritdoc />
    public async Task HandleAsync(RabbitMqSendMessageContext context, Func<Task> next)
    {
        var properties = new BasicProperties
        {
            Headers = context.Headers,
        };

        await _channel.BasicPublishAsync(context.Exchange, context.RoutingKey, _mandatory, properties, context.Body);
        context.Published = true;
    }
}

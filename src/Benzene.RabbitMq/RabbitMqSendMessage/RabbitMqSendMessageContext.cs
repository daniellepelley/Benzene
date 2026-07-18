using RabbitMQ.Client;

namespace Benzene.RabbitMq.RabbitMqSendMessage;

/// <summary>
/// The pipeline context for one outbound RabbitMQ publish: the exchange and routing key to publish
/// to, the serialized body, and the headers to carry on <c>BasicProperties</c>. <see cref="Published"/>
/// records whether the publish completed (set by <c>RabbitMqClientMiddleware</c>).
/// </summary>
public class RabbitMqSendMessageContext
{
    /// <summary>Initializes a new instance of the <see cref="RabbitMqSendMessageContext"/> class.</summary>
    /// <param name="exchange">The exchange to publish to (empty string for the default exchange).</param>
    /// <param name="routingKey">The routing key (for the default exchange, the target queue name).</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="headers">The headers to place on the message's <c>BasicProperties</c>.</param>
    public RabbitMqSendMessageContext(string exchange, string routingKey, ReadOnlyMemory<byte> body, IDictionary<string, object?> headers)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Body = body;
        Headers = headers;
    }

    /// <summary>Gets the exchange to publish to.</summary>
    public string Exchange { get; }

    /// <summary>Gets the routing key to publish with.</summary>
    public string RoutingKey { get; }

    /// <summary>Gets the serialized message body.</summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>Gets the headers to carry on the message's <c>BasicProperties</c>.</summary>
    public IDictionary<string, object?> Headers { get; }

    /// <summary>Gets or sets whether the publish completed successfully.</summary>
    public bool Published { get; set; }
}

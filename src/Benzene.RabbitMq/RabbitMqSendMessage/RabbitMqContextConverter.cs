using System.Text;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.RabbitMq.RabbitMqSendMessage;

/// <summary>
/// Converts a Benzene outbound client request into a <see cref="RabbitMqSendMessageContext"/> and
/// maps the publish outcome back to a Benzene status. The request's <c>Topic</c> becomes the AMQP
/// routing key and is also carried as a <c>"topic"</c> header, so a <see cref="RabbitMqWorker"/>
/// consuming the message routes by header (portable) with the routing key as the idiomatic fallback.
/// The Benzene header dictionary is forwarded onto <c>BasicProperties.Headers</c> (UTF-8 encoded) so
/// correlation/trace/version headers reach the wire - matching Kafka's header forwarding.
/// </summary>
/// <typeparam name="T">The request message type.</typeparam>
public class RabbitMqContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, RabbitMqSendMessageContext>
{
    private readonly ISerializer _serializer;
    private readonly string _exchange;

    /// <summary>Initializes a new instance publishing to the default exchange with the JSON serializer.</summary>
    public RabbitMqContextConverter()
        : this(new JsonSerializer(), string.Empty)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RabbitMqContextConverter{T}"/> class.</summary>
    /// <param name="serializer">The serializer used to encode the message body.</param>
    /// <param name="exchange">The exchange to publish to. Empty string (the default) uses the default exchange, where the routing key is the target queue name.</param>
    public RabbitMqContextConverter(ISerializer serializer, string exchange)
    {
        _serializer = serializer;
        _exchange = exchange;
    }

    /// <inheritdoc />
    public Task<RabbitMqSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var headers = new Dictionary<string, object?>();
        foreach (var header in contextIn.Request.Headers)
        {
            headers[header.Key] = Encoding.UTF8.GetBytes(header.Value);
        }

        // Carry the topic as a header too, so a Benzene RabbitMQ consumer's header-first topic getter
        // round-trips it regardless of the routing key the exchange binding uses.
        headers["topic"] = Encoding.UTF8.GetBytes(contextIn.Request.Topic);

        var body = Encoding.UTF8.GetBytes(_serializer.Serialize(contextIn.Request.Message));

        return Task.FromResult(new RabbitMqSendMessageContext(_exchange, contextIn.Request.Topic, body, headers));
    }

    /// <inheritdoc />
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, RabbitMqSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Published
            ? BenzeneResult.Accepted<Void>()
            : BenzeneResult.ServiceUnavailable<Void>("RabbitMQ message was not published");
        return Task.CompletedTask;
    }
}

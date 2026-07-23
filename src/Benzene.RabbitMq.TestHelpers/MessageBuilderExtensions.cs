using System.Text;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Benzene.RabbitMq.TestHelpers;

/// <summary>
/// Test helpers that turn a <see cref="IMessageBuilder{T}"/> into a <see cref="BasicDeliverEventArgs"/>,
/// so a component test can push the demo message through a <see cref="RabbitMqBenzeneTestHost"/> exactly
/// as the broker would deliver it. The topic rides as the <c>"topic"</c> header (and the AMQP routing
/// key), and the message body is the raw serialized payload.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds a <see cref="BasicDeliverEventArgs"/> from the message, using the default JSON serializer.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <returns>The RabbitMQ delivery.</returns>
    public static BasicDeliverEventArgs AsRabbitMqBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsRabbitMqBenzeneMessage(new JsonSerializer());
    }

    /// <summary>
    /// Builds a <see cref="BasicDeliverEventArgs"/> from the message, using the supplied serializer for
    /// the body.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <param name="serializer">The serializer used to render the delivery body.</param>
    /// <returns>The RabbitMQ delivery.</returns>
    public static BasicDeliverEventArgs AsRabbitMqBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        // RabbitMQ header values are byte[]-encoded on the wire; the topic getter decodes the "topic"
        // header (falling back to the routing key), and the headers getter decodes the rest.
        var headers = new Dictionary<string, object?>
        {
            [RabbitMqConstants.DefaultTopicHeader] = Encoding.UTF8.GetBytes(source.Topic)
        };
        foreach (var header in source.Headers)
        {
            headers[header.Key] = Encoding.UTF8.GetBytes(header.Value);
        }

        var properties = new BasicProperties { Headers = headers };
        var body = Encoding.UTF8.GetBytes(serializer.Serialize(source.Message));

        return new BasicDeliverEventArgs("test-consumer", 1, false, string.Empty, source.Topic, properties, body);
    }
}

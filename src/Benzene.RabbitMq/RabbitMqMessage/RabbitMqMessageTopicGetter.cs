using System.Text;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.RabbitMq.RabbitMqMessage;

/// <summary>
/// Extracts the message topic from a RabbitMQ delivery's <c>"topic"</c> header, falling back to the
/// AMQP routing key when that header isn't present.
/// </summary>
/// <remarks>
/// The header is the portable choice - it matches how every other Benzene queue transport
/// (SQS/SNS/Service Bus/PubSub) carries the topic, so a message published by a Benzene client
/// round-trips unchanged. The routing-key fallback lets a non-Benzene producer (which won't set a
/// <c>"topic"</c> header) still route by its natural RabbitMQ routing key. When neither yields a
/// value, <see cref="Topic"/> resolves the topic id to <see cref="Benzene.Core.Constants.Missing"/>,
/// the same as the other consumer packages. Wrap this in
/// <see cref="Benzene.Core.MessageHandlers.PresetTopicMessageTopicGetter{TContext}"/> (as
/// <see cref="DependencyInjectionExtensions.AddRabbitMq"/> does) to honor <c>.UsePresetTopic(...)</c>.
/// </remarks>
public class RabbitMqMessageTopicGetter : IMessageTopicGetter<RabbitMqContext>
{
    /// <inheritdoc />
    public ITopic GetTopic(RabbitMqContext context)
    {
        return new Topic(GetTopicHeader(context) ?? context.DeliverEventArgs.RoutingKey);
    }

    private static string? GetTopicHeader(RabbitMqContext context)
    {
        var headers = context.DeliverEventArgs.BasicProperties.Headers;
        if (headers == null || !headers.TryGetValue("topic", out var value) || value == null)
        {
            return null;
        }

        // RabbitMQ carries header values as byte[] on the wire; a client may also set a raw string.
        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString(),
        };
    }
}

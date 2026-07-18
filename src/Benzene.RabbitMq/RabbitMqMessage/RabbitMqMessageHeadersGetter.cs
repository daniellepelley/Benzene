using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.RabbitMq.RabbitMqMessage;

/// <summary>
/// Extracts headers from a RabbitMQ delivery's <c>BasicProperties.Headers</c>, UTF-8 decoding the
/// <c>byte[]</c>-valued entries RabbitMQ carries on the wire (and accepting raw string values too).
/// This means header-based decorators - correlation id, W3C trace context, payload version - work on
/// this transport exactly as on the others, since <see cref="RabbitMqSendMessage.RabbitMqContextConverter{T}"/>
/// forwards the Benzene header dictionary onto the same header table when publishing.
/// </summary>
public class RabbitMqMessageHeadersGetter : IMessageHeadersGetter<RabbitMqContext>
{
    /// <inheritdoc />
    public IDictionary<string, string> GetHeaders(RabbitMqContext context)
    {
        var headers = context.DeliverEventArgs.BasicProperties.Headers;
        if (headers == null)
        {
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>();
        foreach (var header in headers)
        {
            if (header.Value == null)
            {
                continue;
            }

            result[header.Key] = header.Value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string text => text,
                _ => header.Value.ToString() ?? string.Empty,
            };
        }

        return result;
    }
}

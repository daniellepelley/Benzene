using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.RabbitMq.RabbitMqMessage;

/// <summary>
/// Extracts the message body from a RabbitMQ delivery, UTF-8 decoding the raw <c>byte[]</c> payload -
/// the same convention every other Benzene transport uses.
/// </summary>
public class RabbitMqMessageBodyGetter : IMessageBodyGetter<RabbitMqContext>
{
    /// <inheritdoc />
    public string? GetBody(RabbitMqContext context)
    {
        return Encoding.UTF8.GetString(context.DeliverEventArgs.Body.Span);
    }
}

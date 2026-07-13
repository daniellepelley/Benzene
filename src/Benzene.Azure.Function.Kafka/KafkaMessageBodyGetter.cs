using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Extracts the message body from a Kafka event's value.
/// </summary>
public class KafkaMessageBodyGetter : IMessageBodyGetter<KafkaContext>
{
    /// <summary>
    /// Gets the Kafka event's value, UTF-8 decoded, as the message body.
    /// </summary>
    /// <param name="context">The Kafka context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(KafkaContext context)
    {
        return context.KafkaEvent.Value == null ? null : Encoding.UTF8.GetString(context.KafkaEvent.Value);
    }
}

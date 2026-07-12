using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Kafka;

/// <summary>
/// Extracts headers from a Kafka event. Currently always returns an empty dictionary, since
/// <see cref="Microsoft.Azure.WebJobs.Extensions.Kafka.KafkaEventData{TValue}"/> headers aren't mapped.
/// </summary>
public class KafkaMessageHeadersGetter : IMessageHeadersGetter<KafkaContext>
{
    /// <summary>
    /// Gets the headers for the Kafka event. Always empty.
    /// </summary>
    /// <param name="context">The Kafka context to extract headers from.</param>
    /// <returns>An empty dictionary.</returns>
    public IDictionary<string, string> GetHeaders(KafkaContext context)
    {
        return new Dictionary<string, string>();
    }
}

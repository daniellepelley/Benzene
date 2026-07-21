using System.Linq;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Extracts headers from a Kafka event's <c>KafkaRecord.Headers</c>, UTF-8 decoding each value.
/// </summary>
public class KafkaMessageHeadersGetter : IMessageHeadersGetter<KafkaContext>
{
    /// <summary>
    /// Gets the decoded headers from the Kafka record.
    /// </summary>
    /// <param name="context">The Kafka context to extract headers from.</param>
    /// <returns>A dictionary of header names to UTF-8 decoded values. Empty when the record has no headers.</returns>
    public IDictionary<string, string> GetHeaders(KafkaContext context)
    {
        var headers = context.KafkaEvent.Headers;

        if (headers == null || headers.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        // Last-wins over the header array (Kafka permits duplicate keys); ToDictionary would throw on a
        // duplicate, aborting the record. Matches the AWS Lambda Kafka and Kafka.Core getters.
        var dictionary = new Dictionary<string, string>();
        foreach (var header in headers)
        {
            dictionary[header.Key] = Encoding.UTF8.GetString(header.Value ?? System.Array.Empty<byte>());
        }

        return dictionary;
    }
}

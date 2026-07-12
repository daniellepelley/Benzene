using System.Collections.Generic;
using System.Linq;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Extracts headers from a Kafka record's first header batch, UTF-8 decoding each value, and adds the
/// Kafka topic name as a <c>topic</c> header.
/// </summary>
public class KafkaMessageHeadersGetter : IMessageHeadersGetter<KafkaContext>
{
    /// <summary>
    /// Gets the decoded headers from the Kafka record, including the topic name.
    /// </summary>
    /// <param name="context">The Kafka context to extract headers from.</param>
    /// <returns>A dictionary of header names to UTF-8 decoded values, plus a <c>topic</c> entry.</returns>
    public IDictionary<string, string> GetHeaders(KafkaContext context)
    {
        var headers = context.KafkaEventRecord.Headers.FirstOrDefault();

        if (headers == null)
        {
            return new Dictionary<string, string>();
        }

        var dictionary = headers
            .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.Value));

        dictionary.Add("topic", context.KafkaEventRecord.Topic);

        return dictionary;
    }
}

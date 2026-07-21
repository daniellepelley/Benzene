using System;
using System.Collections.Generic;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Extracts every header from a Kafka record, UTF-8 decoding each value, and adds the Kafka topic
/// name as a <c>topic</c> header.
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
        var headerBatches = context.KafkaEventRecord.Headers;

        if (headerBatches == null || headerBatches.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        // The AWS Kafka wire format emits each record header as a separate single-entry element in the
        // Headers list (preserving Kafka's ordered, duplicate-key-capable headers). Flatten every
        // element rather than taking only the first (which dropped all headers after the first), and use
        // a last-wins indexer so a duplicate key doesn't throw (Kafka permits repeated keys).
        var dictionary = new Dictionary<string, string>();
        foreach (var batch in headerBatches)
        {
            if (batch == null)
            {
                continue;
            }

            foreach (var header in batch)
            {
                dictionary[header.Key] = Encoding.UTF8.GetString(header.Value ?? Array.Empty<byte>());
            }
        }

        dictionary["topic"] = context.KafkaEventRecord.Topic;

        return dictionary;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Extracts every header from a Kafka record, UTF-8 decoding each value.
/// </summary>
/// <remarks>
/// The Kafka topic is a <em>routing</em> concept resolved by <see cref="KafkaMessageTopicGetter"/>
/// (from the record's own <c>Topic</c>) - it is deliberately NOT surfaced as a <c>topic</c> header.
/// Fabricating one here meant the inbound topic silently rode along when a handler forwarded the
/// message's headers onto an outbound send, which — if that send targets a topic the same service
/// consumes — is an infinite-loop trap. An outbound topic must be set explicitly, never inherited
/// from the message being handled.
/// </remarks>
public class KafkaMessageHeadersGetter : IMessageHeadersGetter<KafkaContext>
{
    /// <summary>
    /// Gets the decoded headers from the Kafka record.
    /// </summary>
    /// <param name="context">The Kafka context to extract headers from.</param>
    /// <returns>A dictionary of header names to UTF-8 decoded values. Empty when the record has no headers.</returns>
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

        return dictionary;
    }
}

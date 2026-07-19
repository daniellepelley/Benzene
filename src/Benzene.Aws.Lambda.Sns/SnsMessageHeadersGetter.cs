using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Extracts headers from an SNS record's message attributes.
/// </summary>
public class SnsMessageHeadersGetter : IMessageHeadersGetter<SnsRecordContext>
{
    /// <summary>
    /// Gets the SNS message attributes as headers.
    /// </summary>
    /// <param name="context">The SNS record context to extract headers from.</param>
    /// <returns>A dictionary of header names to values.</returns>
    public IDictionary<string, string> GetHeaders(SnsRecordContext context)
    {
        // Null-safe, matching SnsUtils.GetFromAttributes (used by the topic getter for the same
        // object): an SNS record deserialized from a payload with no MessageAttributes field has a
        // null MessageAttributes, and the topic path handles that gracefully - the headers path must
        // too, rather than NRE-ing out of the invocation on the same record.
        return context.SnsRecord.Sns?.MessageAttributes?
            .ToDictionary(x => x.Key, x => x.Value.Value)
            ?? new Dictionary<string, string>();
    }
}

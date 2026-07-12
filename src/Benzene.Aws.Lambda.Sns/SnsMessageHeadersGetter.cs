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
        return context.SnsRecord.Sns.MessageAttributes
            .ToDictionary(x => x.Key, x => x.Value.Value);
    }
}

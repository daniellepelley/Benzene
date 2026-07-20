using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Extracts headers from an SQS message's string-typed message attributes.
/// </summary>
public class SqsMessageHeadersGetter : IMessageHeadersGetter<SqsMessageContext>
{
    /// <summary>
    /// Gets the string-typed message attributes as headers.
    /// </summary>
    /// <param name="context">The SQS message context to extract headers from.</param>
    /// <returns>A dictionary of header names to values, limited to attributes with a <c>String</c> data type.</returns>
    public IDictionary<string, string> GetHeaders(SqsMessageContext context)
    {
        // Null-guard the attribute map (a message deserialized from a payload with no attributes can
        // yield null), matching the SNS getter's hardening rather than NRE-ing out of the invocation.
        return context.SqsMessage.MessageAttributes?
            .Where(x => x.Value.DataType == "String")
            .ToDictionary(x => x.Key, x => x.Value.StringValue)
            ?? new Dictionary<string, string>();
    }
}

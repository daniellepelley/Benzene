using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Extracts the raw message body from an SNS record.
/// </summary>
public class SnsMessageBodyGetter : IMessageBodyGetter<SnsRecordContext>
{
    /// <summary>
    /// Gets the raw message body from the SNS record.
    /// </summary>
    /// <param name="context">The SNS record context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(SnsRecordContext context)
    {
        // Null-safe, matching the sibling SnsMessageHeadersGetter/SnsMessageTopicGetter (via SnsUtils):
        // an SNS record deserialized from a payload with no Sns field yields null, and the other getters
        // handle that gracefully rather than NRE-ing out of the invocation on the same record.
        return context.SnsRecord.Sns?.Message;
    }
}

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
        return context.SnsRecord.Sns.Message;
    }
}

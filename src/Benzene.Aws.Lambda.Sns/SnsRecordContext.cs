using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Provides the middleware pipeline context for a single record within an SNS batch event.
/// </summary>
public class SnsRecordContext : IHasMessageResult
{
    private SnsRecordContext(SNSEvent snsEvent, SNSEvent.SNSRecord snsRecord)
    {
        SnsRecord = snsRecord;
        SnsEvent = snsEvent;
    }

    /// <summary>
    /// Creates a new <see cref="SnsRecordContext"/> for a single record within an SNS batch event.
    /// </summary>
    /// <param name="snsEvent">The full SNS batch event.</param>
    /// <param name="snsRecord">The specific record within the batch this context represents.</param>
    /// <returns>The created context.</returns>
    public static SnsRecordContext CreateInstance(SNSEvent snsEvent, SNSEvent.SNSRecord snsRecord)
    {
        return new SnsRecordContext(snsEvent, snsRecord);
    }

    /// <summary>
    /// Gets the full SNS batch event this record belongs to.
    /// </summary>
    public SNSEvent SnsEvent { get; }

    /// <summary>
    /// Gets the specific SNS record this context represents.
    /// </summary>
    public SNSEvent.SNSRecord SnsRecord { get; }

    /// <summary>
    /// Gets or sets the result of handling this record. Set by <see cref="SnsMessageHandlerResultSetter"/>.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}

using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.MessageHandlers.ToDelete;

namespace Benzene.Aws.Lambda.Sns;

public class SnsRecordContext : IHasMessageResult
{
    private SnsRecordContext(SNSEvent snsEvent, SNSEvent.SNSRecord snsRecord)
    {
        SnsRecord = snsRecord;
        SnsEvent = snsEvent;
    }

    public static SnsRecordContext CreateInstance(SNSEvent snsEvent, SNSEvent.SNSRecord snsRecord)
    {
        return new SnsRecordContext(snsEvent, snsRecord);
    }

    public SNSEvent SnsEvent { get; }
    public SNSEvent.SNSRecord SnsRecord { get; }
    public IMessageResult MessageResult { get; set; }
}

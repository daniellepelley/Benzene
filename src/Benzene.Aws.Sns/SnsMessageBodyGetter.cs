using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Aws.Sns;

public class SnsMessageBodyGetter : IMessageBodyGetter<SnsRecordContext>
{
    public string GetBody(SnsRecordContext context)
    {
        return context.SnsRecord.Sns.Message;
    }
}

using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sns;

public class SnsMessageBodyMapper : IMessageBodyMapper<SnsRecordContext>
{
    public string GetBody(SnsRecordContext context)
    {
        return context.SnsRecord.Sns.Message;
    }
}

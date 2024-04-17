using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sqs;

public class SqsMessageBodyMapper : IMessageBodyMapper<SqsMessageContext>
{
    public string GetMessage(SqsMessageContext context)
    {
        return context.SqsMessage.Body;
    }
}

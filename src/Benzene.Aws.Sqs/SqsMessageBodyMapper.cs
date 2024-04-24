using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sqs;

public class SqsMessageBodyMapper : IMessageBodyMapper<SqsMessageContext>
{
    public string GetBody(SqsMessageContext context)
    {
        return context.SqsMessage.Body;
    }
}

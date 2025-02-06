using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsMessageBodyGetter : IMessageBodyGetter<SqsMessageContext>
{
    public string GetBody(SqsMessageContext context)
    {
        return context.SqsMessage.Body;
    }
}

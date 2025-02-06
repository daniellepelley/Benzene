using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageBodyGetter : IMessageBodyGetter<SqsConsumerMessageContext>
{
    public string GetBody(SqsConsumerMessageContext context)
    {
        return context.Message.Body;
    }
}

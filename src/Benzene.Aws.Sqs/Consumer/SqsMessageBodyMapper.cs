using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageBodyGetter : IMessageBodyGetter<SqsConsumerMessageContext>
{
    public string GetBody(SqsConsumerMessageContext context)
    {
        return context.Message.Body;
    }
}

using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageBodyMapper : IMessageBodyMapper<SqsConsumerMessageContext>
{
    public string GetMessage(SqsConsumerMessageContext context)
    {
        return context.Message.Body;
    }
}

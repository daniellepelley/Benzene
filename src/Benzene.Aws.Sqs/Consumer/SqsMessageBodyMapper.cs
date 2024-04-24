using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageBodyMapper : IMessageBodyMapper<SqsConsumerMessageContext>
{
    public string GetBody(SqsConsumerMessageContext context)
    {
        return context.Message.Body;
    }
}

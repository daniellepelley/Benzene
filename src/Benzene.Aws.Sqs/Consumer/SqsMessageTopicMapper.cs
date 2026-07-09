using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageTopicGetter : IMessageTopicGetter<SqsConsumerMessageContext>
{
    public ITopic GetTopic(SqsConsumerMessageContext context)
    {
        return new Topic(GetFromAttributes(context, "topic"));
    }
    
    private static string GetFromAttributes(SqsConsumerMessageContext context, string key)
    {
        if (!context.Message.MessageAttributes.ContainsKey(key))
        {
            return null;
        }

        return context.Message.MessageAttributes[key].StringValue;
    }
}

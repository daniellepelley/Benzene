using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsMessageTopicGetter : IMessageTopicGetter<SqsMessageContext>
{
    public ITopic GetTopic(SqsMessageContext context)
    {
        return new Topic(GetFromAttributes(context, "topic"));
    }

    private static string GetFromAttributes(SqsMessageContext context, string key)
    {
        if (!context.SqsMessage.MessageAttributes.ContainsKey(key))
        {
            return null;
        }

        return context.SqsMessage.MessageAttributes[key].StringValue;
    }
}

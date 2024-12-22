using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sqs;

public class SqsMessageTopicMapper : IMessageTopicMapper<SqsMessageContext>
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

﻿using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageTopicMapper : IMessageTopicMapper<SqsConsumerMessageContext>
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

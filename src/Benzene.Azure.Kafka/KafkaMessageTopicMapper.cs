﻿using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.Kafka;

public class KafkaMessageTopicMapper : IMessageTopicMapper<KafkaContext>
{
    public ITopic GetTopic(KafkaContext context)
    {
        return new Topic(context.KafkaEvent.Topic);
    }
}

using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaSendMessageTopicGetter : IMessageTopicGetter<KafkaSendMessageContext>
{
    public ITopic GetTopic(KafkaSendMessageContext context)
    {
        return new Topic(context.Topic);
    }
}
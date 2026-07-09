using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.Kafka;

public class KafkaMessageTopicGetter : IMessageTopicGetter<KafkaContext>
{
    public ITopic GetTopic(KafkaContext context)
    {
        return new Topic(context.KafkaEvent.Topic);
    }
}

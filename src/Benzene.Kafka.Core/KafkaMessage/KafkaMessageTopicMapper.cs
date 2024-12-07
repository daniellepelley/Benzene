using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageTopicMapper <TKey, TValue>: IMessageTopicMapper<KafkaRecordContext<TKey, TValue>>
{
    public ITopic GetTopic(KafkaRecordContext<TKey, TValue> context)
    {
        return new Topic(context.ConsumeResult.Topic);
    }
}
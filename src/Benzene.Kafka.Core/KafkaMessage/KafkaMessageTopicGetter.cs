using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageTopicGetter <TKey, TValue>: IMessageTopicGetter<KafkaRecordContext<TKey, TValue>>
{
    public ITopic GetTopic(KafkaRecordContext<TKey, TValue> context)
    {
        return new Topic(context.ConsumeResult.Topic);
    }
}
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageBodyGetter<TKey, TValue> : IMessageBodyGetter<KafkaRecordContext<TKey, TValue>>
{
    public string? GetBody(KafkaRecordContext<TKey, TValue> context)
    {
        return context.ConsumeResult.Message.Value.ToString();
    }
}
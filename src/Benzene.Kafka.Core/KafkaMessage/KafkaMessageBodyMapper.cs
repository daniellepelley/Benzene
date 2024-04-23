using Benzene.Abstractions.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageBodyMapper<TKey, TValue> : IMessageBodyMapper<KafkaRecordContext<TKey, TValue>>
{
    public string? GetMessage(KafkaRecordContext<TKey, TValue> context)
    {
        return context.ConsumeResult.Message.Value.ToString();
    }
}
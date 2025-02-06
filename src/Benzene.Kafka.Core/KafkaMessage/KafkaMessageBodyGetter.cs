using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageBodyGetter<TKey, TValue> : IMessageBodyGetter<KafkaRecordContext<TKey, TValue>>
{
    public string? GetBody(KafkaRecordContext<TKey, TValue> context)
    {
        return context.ConsumeResult.Message.Value.ToString();
    }
}
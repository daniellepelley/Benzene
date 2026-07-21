using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaRecordContext<TKey, TValue> :  IHasMessageResult
{
    public KafkaRecordContext(ConsumeResult<TKey, TValue> consumeResult)
    {
        ConsumeResult = consumeResult;
    }

    public ConsumeResult<TKey, TValue> ConsumeResult { get; }
    public IBenzeneResult MessageResult { get; set; }
}
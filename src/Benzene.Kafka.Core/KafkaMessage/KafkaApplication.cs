using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaApplication<TKey, TValue> : MiddlewareApplication<ConsumeResult<TKey, TValue>, KafkaRecordContext<TKey, TValue>>
{
    public KafkaApplication(IMiddlewarePipeline<KafkaRecordContext<TKey, TValue>> pipelineBuilder)
        :base(pipelineBuilder, @event => new KafkaRecordContext<TKey, TValue>(@event))
    { }
}
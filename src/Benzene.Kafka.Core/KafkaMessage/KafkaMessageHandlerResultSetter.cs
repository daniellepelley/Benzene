using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageHandlerResultSetter<TKey, TValue> : IMessageHandlerResultSetter<KafkaRecordContext<TKey, TValue>>
{
    public Task SetResultAsync(KafkaRecordContext<TKey, TValue> context, IMessageHandlerResult messageHandlerResult)
    {
        context.MessageResult = messageHandlerResult.BenzeneResult;
        return Task.CompletedTask;
    }
}
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageHeadersGetter<TKey, TValue> : IMessageHeadersGetter<KafkaRecordContext<TKey, TValue>>
{
    public IDictionary<string, string> GetHeaders(KafkaRecordContext<TKey, TValue> context)
    {
        return context.ConsumeResult.Message.Headers
            .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()));
    }
}
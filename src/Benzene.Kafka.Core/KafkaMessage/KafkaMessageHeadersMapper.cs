using System.Text;
using Benzene.Abstractions.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageHeadersMapper<TKey, TValue> : IMessageHeadersMapper<KafkaRecordContext<TKey, TValue>>
{
    public IDictionary<string, string> GetHeaders(KafkaRecordContext<TKey, TValue> context)
    {
        return context.ConsumeResult.Message.Headers
            .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()));
    }
}
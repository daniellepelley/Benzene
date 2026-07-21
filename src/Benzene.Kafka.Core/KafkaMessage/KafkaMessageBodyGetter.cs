using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageBodyGetter<TKey, TValue> : IMessageBodyGetter<KafkaRecordContext<TKey, TValue>>
{
    public string? GetBody(KafkaRecordContext<TKey, TValue> context)
    {
        // A raw byte[] value (a common Kafka TValue) must be UTF-8 decoded, not .ToString()'d - the
        // latter yields "System.Byte[]". A string passes through; anything else falls back to ToString.
        return context.ConsumeResult.Message.Value switch
        {
            null => null,
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            var value => value.ToString()
        };
    }
}
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.KafkaMessage;

public class KafkaMessageHeadersGetter<TKey, TValue> : IMessageHeadersGetter<KafkaRecordContext<TKey, TValue>>
{
    public IDictionary<string, string> GetHeaders(KafkaRecordContext<TKey, TValue> context)
    {
        // Kafka headers are an ordered list that legitimately permits repeated keys, so build the
        // dictionary with a last-wins indexer rather than ToDictionary (which throws on a duplicate
        // key, making a valid record unprocessable). Matches the RabbitMq/gRPC header getters.
        var dictionary = new Dictionary<string, string>();
        foreach (var header in context.ConsumeResult.Message.Headers)
        {
            dictionary[header.Key] = Encoding.UTF8.GetString(header.GetValueBytes() ?? System.Array.Empty<byte>());
        }

        return dictionary;
    }
}
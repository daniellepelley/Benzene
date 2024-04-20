using Benzene.Abstractions.Mappers;

namespace Benzene.Azure.Kafka;

public class KafkaMessageHeadersMapper : IMessageHeadersMapper<KafkaContext>
{
    public IDictionary<string, string> GetHeaders(KafkaContext context)
    {
        return new Dictionary<string, string>();
    }
}

using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Kafka;

public class KafkaMessageHeadersGetter : IMessageHeadersGetter<KafkaContext>
{
    public IDictionary<string, string> GetHeaders(KafkaContext context)
    {
        return new Dictionary<string, string>();
    }
}

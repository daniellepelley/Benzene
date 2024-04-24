using Benzene.Abstractions.Mappers;

namespace Benzene.Azure.Kafka;

public class KafkaMessageBodyMapper : IMessageBodyMapper<KafkaContext>
{
    public string GetBody(KafkaContext context)
    {
        return context.KafkaEvent.Value;
    }
}

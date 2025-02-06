using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Kafka;

public class KafkaMessageBodyGetter : IMessageBodyGetter<KafkaContext>
{
    public string GetBody(KafkaContext context)
    {
        return context.KafkaEvent.Value;
    }
}

using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaSendMessageBodyGetter : IMessageBodyGetter<KafkaSendMessageContext>
{
    public string? GetBody(KafkaSendMessageContext context)
    {
        return context.Message.Value;
    }
}
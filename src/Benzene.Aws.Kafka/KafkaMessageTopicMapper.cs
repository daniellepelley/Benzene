using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Mappers;

namespace Benzene.Aws.Kafka;

public class KafkaMessageTopicMapper : IMessageTopicMapper<KafkaContext>
{
    public ITopic GetTopic(KafkaContext context)
    {
        return new Topic(context.KafkaEventRecord.Topic);
    }
}
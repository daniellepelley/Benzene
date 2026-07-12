using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Extracts the message topic from a Kafka record's topic name.
/// </summary>
public class KafkaMessageTopicGetter : IMessageTopicGetter<KafkaContext>
{
    /// <summary>
    /// Gets the topic from the Kafka record.
    /// </summary>
    /// <param name="context">The Kafka context to extract the topic from.</param>
    /// <returns>The Kafka topic.</returns>
    public ITopic GetTopic(KafkaContext context)
    {
        return new Topic(context.KafkaEventRecord.Topic);
    }
}

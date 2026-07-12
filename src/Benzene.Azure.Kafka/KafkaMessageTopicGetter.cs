using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.Kafka;

/// <summary>
/// Extracts the message topic from a Kafka event's topic name.
/// </summary>
public class KafkaMessageTopicGetter : IMessageTopicGetter<KafkaContext>
{
    /// <summary>
    /// Gets the topic from the Kafka event's topic name.
    /// </summary>
    /// <param name="context">The Kafka context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(KafkaContext context)
    {
        return new Topic(context.KafkaEvent.Topic);
    }
}

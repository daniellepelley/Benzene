using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Extracts the message topic from a Pub/Sub message's <c>"topic"</c> attribute - the same
/// "topic in a custom attribute/property" convention already used by
/// <c>Benzene.Aws.Sqs</c>/<c>Benzene.Aws.Lambda.Sqs</c>/<c>Benzene.Aws.Lambda.Sns</c>/
/// <c>Benzene.Azure.Function.ServiceBus</c>, since Pub/Sub has no native per-message "topic"
/// concept of its own (a Pub/Sub topic is the publish destination, not a per-message routing key).
/// </summary>
public class PubSubMessageTopicGetter : IMessageTopicGetter<PubSubContext>
{
    /// <summary>
    /// Gets the topic from the Pub/Sub message's <c>"topic"</c> attribute.
    /// </summary>
    /// <param name="context">The Pub/Sub context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(PubSubContext context)
    {
        return new Topic(GetTopicAttribute(context));
    }

    private static string GetTopicAttribute(PubSubContext context)
    {
        return context.Message.Attributes.TryGetValue("topic", out var value) ? value : null;
    }
}

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
    /// The default message-attribute key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="PubSubMessageTopicGetter(string)"/> (or
    /// via <c>DependencyInjectionExtensions.AddGooglePubSub(topicAttributeKey)</c> /
    /// <c>UsePubSub(..., topicAttributeKey)</c>) to consume messages a non-Benzene producer routes on
    /// another attribute.
    /// </summary>
    public const string DefaultTopicAttribute = "topic";

    private readonly string _topicAttributeKey;

    /// <summary>
    /// Initializes a new instance that reads the topic from the given message-attribute key.
    /// </summary>
    /// <param name="topicAttributeKey">
    /// The message attribute the topic is carried on. Defaults to
    /// <see cref="DefaultTopicAttribute"/> (<c>"topic"</c>).
    /// </param>
    public PubSubMessageTopicGetter(string topicAttributeKey = DefaultTopicAttribute)
    {
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Gets the topic from the Pub/Sub message's topic attribute.
    /// </summary>
    /// <param name="context">The Pub/Sub context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(PubSubContext context)
    {
        return new Topic(GetTopicAttribute(context));
    }

    private string GetTopicAttribute(PubSubContext context)
    {
        return context.Message.Attributes.TryGetValue(_topicAttributeKey, out var value) ? value : null;
    }
}

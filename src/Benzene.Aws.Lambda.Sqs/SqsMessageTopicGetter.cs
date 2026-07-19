using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Extracts the message topic from an SQS message's topic message attribute.
/// </summary>
public class SqsMessageTopicGetter : IMessageTopicGetter<SqsMessageContext>
{
    /// <summary>
    /// The default message-attribute key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="SqsMessageTopicGetter(string)"/> (or via
    /// <c>DependencyInjectionExtensions.AddSqs(topicAttributeKey)</c> / <c>Extensions.UseSqs(...,
    /// topicAttributeKey)</c>) to consume messages a non-Benzene producer routes on another attribute.
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
    public SqsMessageTopicGetter(string topicAttributeKey = DefaultTopicAttribute)
    {
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Gets the topic from the SQS message's topic attribute.
    /// </summary>
    /// <param name="context">The SQS message context to extract the topic from.</param>
    /// <returns>The topic, or a topic with a null ID if the topic attribute isn't present.</returns>
    public ITopic GetTopic(SqsMessageContext context)
    {
        return new Topic(GetFromAttributes(context, _topicAttributeKey));
    }

    private static string GetFromAttributes(SqsMessageContext context, string key)
    {
        if (!context.SqsMessage.MessageAttributes.ContainsKey(key))
        {
            return null;
        }

        return context.SqsMessage.MessageAttributes[key].StringValue;
    }
}

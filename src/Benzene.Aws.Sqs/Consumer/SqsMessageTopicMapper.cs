using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Extracts the message topic from an SQS message's topic message attribute.
/// </summary>
public class SqsConsumerMessageTopicGetter : IMessageTopicGetter<SqsConsumerMessageContext>
{
    /// <summary>
    /// The default message-attribute key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="SqsConsumerMessageTopicGetter(string)"/>
    /// (or via <c>DependencyInjectionExtensions.AddSqsConsumer(topicAttributeKey)</c>) to consume
    /// messages a non-Benzene producer routes on another attribute.
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
    public SqsConsumerMessageTopicGetter(string topicAttributeKey = DefaultTopicAttribute)
    {
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Gets the topic from the SQS message's topic attribute.
    /// </summary>
    /// <param name="context">The SQS consumer message context to extract the topic from.</param>
    /// <returns>
    /// The topic, or a topic with <see cref="Benzene.Core.Constants.Missing"/> as its ID if the
    /// topic attribute isn't present.
    /// </returns>
    public ITopic GetTopic(SqsConsumerMessageContext context)
    {
        return new Topic(GetFromAttributes(context, _topicAttributeKey));
    }

    private static string GetFromAttributes(SqsConsumerMessageContext context, string key)
    {
        if (!context.Message.MessageAttributes.ContainsKey(key))
        {
            return null;
        }

        return context.Message.MessageAttributes[key].StringValue;
    }
}

using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Extracts the message topic from an SNS record's topic message attribute.
/// </summary>
public class SnsMessageTopicGetter : IMessageTopicGetter<SnsRecordContext>
{
    /// <summary>
    /// The default message-attribute key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="SnsMessageTopicGetter(string)"/> (or via
    /// <c>DependencyInjectionExtensions.AddSns(topicAttributeKey)</c> / <c>Extensions.UseSns(...,
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
    public SnsMessageTopicGetter(string topicAttributeKey = DefaultTopicAttribute)
    {
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Gets the topic from the SNS record's topic attribute.
    /// </summary>
    /// <param name="context">The SNS record context to extract the topic from.</param>
    /// <returns>The topic, or a topic with a null ID if the topic attribute isn't present.</returns>
    public ITopic GetTopic(SnsRecordContext context)
    {
        return new Topic(SnsUtils.GetFromAttributes(context, _topicAttributeKey));
    }
}

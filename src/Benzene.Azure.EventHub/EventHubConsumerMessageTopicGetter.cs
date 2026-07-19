using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Extracts the message topic from an event's topic property.
/// </summary>
public class EventHubConsumerMessageTopicGetter : IMessageTopicGetter<EventHubConsumerContext>
{
    /// <summary>
    /// The default event-property key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="EventHubConsumerMessageTopicGetter(string)"/>
    /// (or via <see cref="BenzeneEventHubConfig.TopicPropertyKey"/> /
    /// <c>DependencyInjectionExtensions.AddEventHubConsumer(topicPropertyKey)</c>) to consume events a
    /// non-Benzene producer routes on another property.
    /// </summary>
    public const string DefaultTopicProperty = "topic";

    private readonly string _topicPropertyKey;

    /// <summary>
    /// Initializes a new instance that reads the topic from the given event-property key.
    /// </summary>
    /// <param name="topicPropertyKey">
    /// The event property the topic is carried on. Defaults to
    /// <see cref="DefaultTopicProperty"/> (<c>"topic"</c>).
    /// </param>
    public EventHubConsumerMessageTopicGetter(string topicPropertyKey = DefaultTopicProperty)
    {
        _topicPropertyKey = topicPropertyKey;
    }

    /// <summary>
    /// Gets the topic from the event's topic property.
    /// </summary>
    /// <param name="context">The Event Hub consumer context to extract the topic from.</param>
    /// <returns>
    /// The topic, or a topic with <see cref="Benzene.Core.Constants.Missing"/> as its ID if the
    /// topic property isn't present.
    /// </returns>
    public ITopic GetTopic(EventHubConsumerContext context)
    {
        // Topic(null) resolves to Constants.Missing, same as the other consumer packages.
        return new Topic(GetTopicProperty(context)!);
    }

    private string? GetTopicProperty(EventHubConsumerContext context)
    {
        return context.EventData.Properties.TryGetValue(_topicPropertyKey, out var value) ? value as string : null;
    }
}

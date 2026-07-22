using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Extracts the message topic from an event's topic application property — the property-based routing
/// path, mirroring <c>Benzene.Azure.Function.ServiceBus</c>'s <c>ServiceBusMessageTopicGetter</c>. This is
/// what lets an Event Hub trigger route with <c>UseMessageHandlers()</c> directly on
/// <see cref="EventHubContext"/> (matching the <c>OutboundContext</c> Event Hub sender, which writes the
/// topic to the same property), as an alternative to the Benzene-message-envelope <c>UseBenzeneMessage</c> path.
/// </summary>
public class EventHubMessageTopicGetter : IMessageTopicGetter<EventHubContext>
{
    /// <summary>
    /// The default event-property key the topic is read from. Kept in sync with the sender's
    /// <c>OutboundEventHubContextConverter.DefaultTopicProperty</c> (<c>"topic"</c>).
    /// </summary>
    public const string DefaultTopicProperty = "topic";

    private readonly string _topicPropertyKey;

    /// <summary>
    /// Initializes a new instance that reads the topic from the given event-property key.
    /// </summary>
    /// <param name="topicPropertyKey">The event property the topic is carried on (defaults to <see cref="DefaultTopicProperty"/>).</param>
    public EventHubMessageTopicGetter(string topicPropertyKey = DefaultTopicProperty)
    {
        _topicPropertyKey = topicPropertyKey;
    }

    /// <summary>
    /// Gets the topic from the event's topic application property.
    /// </summary>
    /// <param name="context">The Event Hub context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(EventHubContext context)
    {
        return new Topic(context.EventData.Properties.TryGetValue(_topicPropertyKey, out var value) ? value as string : null);
    }
}

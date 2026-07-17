using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Extracts the message topic from an event's <c>"topic"</c> property.
/// </summary>
public class EventHubConsumerMessageTopicGetter : IMessageTopicGetter<EventHubConsumerContext>
{
    /// <summary>
    /// Gets the topic from the event's <c>"topic"</c> property.
    /// </summary>
    /// <param name="context">The Event Hub consumer context to extract the topic from.</param>
    /// <returns>
    /// The topic, or a topic with <see cref="Benzene.Core.Constants.Missing"/> as its ID if the
    /// <c>"topic"</c> property isn't present.
    /// </returns>
    public ITopic GetTopic(EventHubConsumerContext context)
    {
        // Topic(null) resolves to Constants.Missing, same as the other consumer packages.
        return new Topic(GetTopicProperty(context)!);
    }

    private static string? GetTopicProperty(EventHubConsumerContext context)
    {
        return context.EventData.Properties.TryGetValue("topic", out var value) ? value as string : null;
    }
}

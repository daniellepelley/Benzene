using Azure.Messaging.EventHubs;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.EventHub.TestHelpers;

/// <summary>
/// Test helpers that turn a <see cref="IMessageBuilder{T}"/> into an <see cref="EventData"/> for the
/// self-hosted Event Hub consumer, so a component test can push the demo message through an
/// <see cref="EventHubWorkerBenzeneTestHost"/> exactly as the hub would deliver it. The consumer routes
/// by the <c>"topic"</c> event property (unlike the Azure Functions Event Hub trigger's envelope body),
/// so the topic rides as a property and the message body is the raw serialized payload.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds an <see cref="EventData"/> for the self-hosted consumer from the message, using the
    /// default JSON serializer.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <returns>The Event Hub event.</returns>
    public static EventData AsEventHubBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsEventHubBenzeneMessage(new JsonSerializer());
    }

    /// <summary>
    /// Builds an <see cref="EventData"/> for the self-hosted consumer from the message, using the
    /// supplied serializer for the body.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <param name="serializer">The serializer used to render the event body.</param>
    /// <returns>The Event Hub event.</returns>
    public static EventData AsEventHubBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        var eventData = new EventData(new BinaryData(serializer.Serialize(source.Message)));
        eventData.Properties[EventHubConsumerMessageTopicGetter.DefaultTopicProperty] = source.Topic;
        foreach (var header in source.Headers)
        {
            eventData.Properties[header.Key] = header.Value;
        }

        return eventData;
    }
}

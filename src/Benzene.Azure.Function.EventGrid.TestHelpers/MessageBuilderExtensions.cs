using System.Text.Json;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;

namespace Benzene.Azure.Function.EventGrid.TestHelpers;

/// <summary>
/// Test helpers that turn a <see cref="IMessageBuilder{T}"/> into an <see cref="EventGridTriggerEvent"/>,
/// so a component test can push the demo message through a built Azure Function app's Event Grid entry
/// point (<c>HandleEventGridEvents</c>) exactly as the trigger would deliver it.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds an <see cref="EventGridTriggerEvent"/> from the message, using the default JSON serializer.
    /// The builder's topic becomes the event type (Event Grid routes by event type), and the message
    /// body becomes the event's <c>data</c> payload.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <returns>The Event Grid event.</returns>
    public static EventGridTriggerEvent AsEventGridBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsEventGridBenzeneMessage(new global::Benzene.Core.MessageHandlers.Serialization.JsonSerializer());
    }

    /// <summary>
    /// Builds an <see cref="EventGridTriggerEvent"/> from the message, using the supplied serializer for
    /// the <c>data</c> payload. The builder's topic becomes the event type.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <param name="serializer">The serializer used to render the event's <c>data</c> payload.</param>
    /// <returns>The Event Grid event.</returns>
    public static EventGridTriggerEvent AsEventGridBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        using var data = JsonDocument.Parse(serializer.Serialize(source.Message));
        return new EventGridTriggerEvent
        {
            EventType = source.Topic,
            Data = data.RootElement.Clone()
        };
    }
}

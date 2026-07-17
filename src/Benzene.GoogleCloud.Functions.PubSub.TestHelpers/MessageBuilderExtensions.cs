using Benzene.Abstractions;
using Benzene.Core.MessageHandlers.Serialization;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Google.Protobuf;

namespace Benzene.GoogleCloud.Functions.PubSub.TestHelpers;

/// <summary>
/// Bridges the shared <see cref="IMessageBuilder{T}"/> test-message abstraction (used identically
/// across every transport's test helpers) into a Pub/Sub-shaped <see cref="MessagePublishedData"/>.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Converts <paramref name="source"/> into a <see cref="MessagePublishedData"/>, matching
    /// <see cref="PubSubMessageTopicGetter"/>'s <c>"topic"</c>-attribute convention and
    /// <see cref="PubSubMessageHeadersGetter"/>'s attributes-as-headers convention.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="source">The message builder to convert.</param>
    /// <returns>A <see cref="MessagePublishedData"/> ready to dispatch via <c>SendPubSubAsync</c>.</returns>
    public static MessagePublishedData AsPubSubEvent<T>(this IMessageBuilder<T> source)
    {
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(new JsonSerializer().Serialize(source.Message)),
            MessageId = Guid.NewGuid().ToString()
        };

        message.Attributes["topic"] = source.Topic;
        foreach (var header in source.Headers)
        {
            message.Attributes[header.Key] = header.Value;
        }

        return new MessagePublishedData
        {
            Message = message,
            Subscription = "projects/test-project/subscriptions/test-subscription"
        };
    }
}

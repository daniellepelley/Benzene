using System.Text.Json;
using Benzene.GoogleCloud.Functions.PubSub;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Google.Protobuf;

namespace Benzene.GoogleCloud.Functions.PubSub.TestHelpers;

/// <summary>
/// Builds a <see cref="MessagePublishedData"/> for tests that dispatch directly into an
/// <c>ICloudEventFunction&lt;MessagePublishedData&gt;</c> via
/// <see cref="BenzeneTestHostExtensions.SendPubSubAsync"/>, without a live Cloud Functions Framework
/// host or a real Pub/Sub subscription.
/// </summary>
public class PubSubMessageBuilder
{
    private readonly IDictionary<string, string> _attributes = new Dictionary<string, string>();
    private string _body = string.Empty;
    private string _messageId = Guid.NewGuid().ToString();
    private string _subscription = "projects/test-project/subscriptions/test-subscription";

    /// <summary>Serializes <paramref name="message"/> as JSON and uses it as the message body.</summary>
    /// <param name="message">The object to serialize as the message body.</param>
    /// <returns>This instance, for method chaining.</returns>
    public PubSubMessageBuilder WithBody(object message)
    {
        _body = JsonSerializer.Serialize(message);
        return this;
    }

    /// <summary>Uses <paramref name="body"/> verbatim as the message body.</summary>
    /// <param name="body">The raw message body.</param>
    /// <returns>This instance, for method chaining.</returns>
    public PubSubMessageBuilder WithRawBody(string body)
    {
        _body = body;
        return this;
    }

    /// <summary>Adds a message attribute.</summary>
    /// <param name="key">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>This instance, for method chaining.</returns>
    public PubSubMessageBuilder WithAttribute(string key, string value)
    {
        _attributes[key] = value;
        return this;
    }

    /// <summary>Sets the <c>"topic"</c> attribute <see cref="PubSubMessageTopicGetter"/> reads for routing.</summary>
    /// <param name="topic">The topic to route this message to.</param>
    /// <returns>This instance, for method chaining.</returns>
    public PubSubMessageBuilder WithTopic(string topic) => WithAttribute("topic", topic);

    /// <summary>Sets the Pub/Sub message ID.</summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>This instance, for method chaining.</returns>
    public PubSubMessageBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    /// <summary>Sets the subscription this message is being delivered on.</summary>
    /// <param name="subscription">The fully-qualified subscription name.</param>
    /// <returns>This instance, for method chaining.</returns>
    public PubSubMessageBuilder WithSubscription(string subscription)
    {
        _subscription = subscription;
        return this;
    }

    /// <summary>Builds the <see cref="MessagePublishedData"/>.</summary>
    /// <returns>A <see cref="MessagePublishedData"/> with the configured message and subscription.</returns>
    public MessagePublishedData Build()
    {
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(_body),
            MessageId = _messageId
        };

        foreach (var attribute in _attributes)
        {
            message.Attributes[attribute.Key] = attribute.Value;
        }

        return new MessagePublishedData
        {
            Message = message,
            Subscription = _subscription
        };
    }
}

using Google.Cloud.PubSub.V1;

namespace Benzene.Clients.GoogleCloud.PubSub;

/// <summary>
/// Provides the middleware pipeline context for publishing a single message to a Google Cloud Pub/Sub
/// topic — the outbound counterpart of the inbound <c>PubSubContext</c> in
/// <c>Benzene.GoogleCloud.Functions.PubSub</c>.
/// </summary>
public class PubSubSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubSendMessageContext"/> class.
    /// </summary>
    /// <param name="topicName">The Pub/Sub topic to publish to.</param>
    /// <param name="message">The Pub/Sub message to publish.</param>
    public PubSubSendMessageContext(TopicName topicName, PubsubMessage message)
    {
        TopicName = topicName;
        Message = message;
    }

    /// <summary>Gets the Pub/Sub topic to publish to.</summary>
    public TopicName TopicName { get; }

    /// <summary>Gets the Pub/Sub message to publish.</summary>
    public PubsubMessage Message { get; }

    /// <summary>
    /// Gets or sets the server-assigned message id returned by the publish. Set by
    /// <see cref="PubSubClientMiddleware"/>; empty until the message is published.
    /// </summary>
    public string MessageId { get; set; } = "";
}

using Benzene.Abstractions.MessageHandlers;
using Google.Events.Protobuf.Cloud.PubSub.V1;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Provides the middleware pipeline context for a single Pub/Sub message delivered to a Cloud
/// Functions Gen2 CloudEvent trigger - Google Cloud Functions delivers exactly one Pub/Sub message
/// per invocation, unlike AWS/Azure's batch-oriented triggers.
/// </summary>
public class PubSubContext : IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubContext"/> class.
    /// </summary>
    /// <param name="data">The Pub/Sub CloudEvent payload for this invocation.</param>
    public PubSubContext(MessagePublishedData data)
    {
        Data = data;
    }

    /// <summary>
    /// Gets the Pub/Sub CloudEvent payload for this invocation, including the message itself
    /// (<see cref="MessagePublishedData.Message"/>) and the subscription it was delivered on
    /// (<see cref="MessagePublishedData.Subscription"/>).
    /// </summary>
    public MessagePublishedData Data { get; }

    /// <summary>Gets the Pub/Sub message this invocation was delivered for.</summary>
    public PubsubMessage Message => Data.Message;

    /// <summary>
    /// Gets or sets the result of handling this message.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}

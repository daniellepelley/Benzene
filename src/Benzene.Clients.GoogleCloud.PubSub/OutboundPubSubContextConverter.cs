using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Results;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.GoogleCloud.PubSub;

/// <summary>
/// Converts an outbound <see cref="OutboundContext"/> to a <see cref="PubSubSendMessageContext"/>, so an
/// outbound route (<c>OutboundRoutingBuilder.Route</c>) can publish via Pub/Sub. The
/// <see cref="OutboundContext"/> counterpart of the inbound <c>PubSubMessageTopicGetter</c>: it writes
/// the Benzene routing topic to the same <c>"topic"</c> message attribute the inbound adapter reads.
/// </summary>
/// <remarks>
/// Pub/Sub is fire-and-acknowledge (a successful publish returns a server message id, no payload), so a
/// topic routed here must be sent via <c>IBenzeneMessageSender.SendAsync&lt;TRequest, Void&gt;</c>.
/// </remarks>
public class OutboundPubSubContextConverter : IContextConverter<OutboundContext, PubSubSendMessageContext>
{
    /// <summary>
    /// The default message-attribute key the topic is written to — kept in sync with the inbound
    /// consumer's attribute key (Pub/Sub routes by the <c>"topic"</c> attribute, not the Pub/Sub topic
    /// name). Pass a different key to interoperate with a consumer routing on another attribute.
    /// </summary>
    public const string DefaultTopicAttribute = "topic";

    private readonly ISerializer _serializer;
    private readonly TopicName _topicName;
    private readonly string _topicAttributeKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundPubSubContextConverter"/> class using a
    /// JSON serializer for the outgoing message body.
    /// </summary>
    /// <param name="topic">The Pub/Sub topic to publish to — a full resource path
    /// (<c>projects/{project}/topics/{topic}</c>) or a bare topic id resolved against the
    /// <c>GOOGLE_CLOUD_PROJECT</c> environment variable.</param>
    /// <param name="topicAttributeKey">The message attribute the routing topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public OutboundPubSubContextConverter(string topic, string topicAttributeKey = DefaultTopicAttribute)
        : this(topic, new JsonSerializer(), topicAttributeKey)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundPubSubContextConverter"/> class.
    /// </summary>
    /// <param name="topic">The Pub/Sub topic to publish to (full resource path or bare topic id).</param>
    /// <param name="serializer">The serializer used to serialize the outgoing message body.</param>
    /// <param name="topicAttributeKey">The message attribute the routing topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public OutboundPubSubContextConverter(string topic, ISerializer serializer, string topicAttributeKey = DefaultTopicAttribute)
    {
        _topicName = ResolveTopicName(topic);
        _serializer = serializer;
        _topicAttributeKey = topicAttributeKey;
    }

    // Accept either a full "projects/.../topics/..." path (what Terraform outputs / env vars carry) or a
    // bare topic id combined with the GOOGLE_CLOUD_PROJECT env var (convenient for local/dev).
    private static TopicName ResolveTopicName(string topic)
    {
        if (topic.StartsWith("projects/", StringComparison.Ordinal))
        {
            return TopicName.Parse(topic);
        }
        var project = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ?? "local-project";
        return TopicName.FromProjectTopic(project, topic);
    }

    /// <summary>
    /// Builds a Pub/Sub publish request, serializing the message as the body and writing the routing
    /// topic and headers as message attributes.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="PubSubSendMessageContext"/>.</returns>
    public Task<PubSubSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(_serializer.Serialize(contextIn.Request))
        };

        foreach (var header in contextIn.Headers)
        {
            if (!string.IsNullOrEmpty(header.Value))
            {
                message.Attributes[header.Key] = header.Value;
            }
        }

        if (!string.IsNullOrEmpty(contextIn.Topic))
        {
            message.Attributes[_topicAttributeKey] = contextIn.Topic;
        }

        return Task.FromResult(new PubSubSendMessageContext(_topicName, message));
    }

    /// <summary>
    /// Records a successful publish as an accepted result on the outbound context. (A failed publish
    /// throws in <see cref="PubSubClientMiddleware"/> before this runs.)
    /// </summary>
    /// <param name="contextIn">The outbound context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="PubSubSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, PubSubSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}

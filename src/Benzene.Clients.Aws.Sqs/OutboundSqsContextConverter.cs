using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an <see cref="SqsSendMessageContext"/>,
/// so an outbound route (<c>OutboundRoutingBuilder.Route</c>) can send via SQS. The
/// <see cref="OutboundContext"/> counterpart of <see cref="SqsContextConverter{T}"/> - see
/// <c>work/benzene-clients-redesign-plan.md</c> §3.
/// </summary>
/// <remarks>
/// SQS has no request/response semantics beyond a send acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be
/// sent via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundSqsContextConverter : IContextConverter<OutboundContext, SqsSendMessageContext>
{
    /// <summary>
    /// The default message-attribute key the topic is written to. It is a single default, not a
    /// hard-coded value — pass a different key to interoperate with a consumer that routes on another
    /// attribute. Keep it in sync with the consumer's attribute key.
    /// </summary>
    public const string DefaultTopicAttribute = "topic";

    private readonly ISerializer _serializer;
    private readonly string _queueUrl;
    private readonly string _topicAttributeKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundSqsContextConverter"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public OutboundSqsContextConverter(string queueUrl, string topicAttributeKey = DefaultTopicAttribute)
        : this(queueUrl, new JsonSerializer(), topicAttributeKey)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundSqsContextConverter"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public OutboundSqsContextConverter(string queueUrl, ISerializer serializer, string topicAttributeKey = DefaultTopicAttribute)
    {
        _queueUrl = queueUrl;
        _serializer = serializer;
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Builds an SQS send message request, serializing the outgoing message as the message body and
    /// setting the topic and headers as message attributes.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the built <see cref="SqsSendMessageContext"/>.</returns>
    public Task<SqsSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>();
        foreach (var header in contextIn.Headers)
        {
            // SQS rejects a message attribute whose value is empty, so skip empty headers rather than
            // fail the whole send (LocalStack tolerates them, but real SQS does not).
            if (!string.IsNullOrEmpty(header.Value))
            {
                messageAttributes[header.Key] = new MessageAttributeValue { StringValue = header.Value, DataType = "String" };
            }
        }

        // Only emit the routing-topic attribute for a non-empty topic: SQS rejects an empty attribute
        // value, and an empty topic carries no routing information anyway.
        if (!string.IsNullOrEmpty(contextIn.Topic))
        {
            messageAttributes[_topicAttributeKey] = new MessageAttributeValue { StringValue = contextIn.Topic, DataType = "String" };
        }

        SqsContextConverter<object>.GuardAttributeLimit(messageAttributes.Count);

        return Task.FromResult(new SqsSendMessageContext(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _serializer.Serialize(contextIn.Request),
            MessageAttributes = messageAttributes
        }));
    }

    /// <summary>
    /// Maps the SQS send response's HTTP status code back onto the outbound context as an
    /// <see cref="IBenzeneResult{Void}"/>.
    /// </summary>
    /// <param name="contextIn">The outbound context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="SqsSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, SqsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an <see cref="SnsSendMessageContext"/>,
/// so an outbound route (<c>OutboundRoutingBuilder.Route</c>) can publish via SNS. The
/// <see cref="OutboundContext"/> counterpart of <see cref="SnsContextConverter{T}"/> - see
/// <c>work/benzene-clients-redesign-plan.md</c> §3.
/// </summary>
/// <remarks>
/// SNS has no request/response semantics beyond a publish acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be
/// sent via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundSnsContextConverter : IContextConverter<OutboundContext, SnsSendMessageContext>
{
    /// <summary>
    /// The default message-attribute key the Benzene routing topic is written to. It is a single
    /// default, not a hard-coded value — pass a different key to interoperate with a consumer that
    /// routes on another attribute. Keep it in sync with the consumer's attribute key.
    /// </summary>
    public const string DefaultTopicAttribute = "topic";

    private readonly ISerializer _serializer;
    private readonly string _topicArn;
    private readonly string _topicAttributeKey;
    private readonly SnsPublishOptions? _publishOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundSnsContextConverter"/> class, using the
    /// default JSON serializer.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="topicAttributeKey">The message attribute the Benzene topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    /// <param name="publishOptions">Optional FIFO/numeric-typing publish options.</param>
    public OutboundSnsContextConverter(string topicArn, string topicAttributeKey = DefaultTopicAttribute, SnsPublishOptions? publishOptions = null)
        : this(topicArn, new JsonSerializer(), topicAttributeKey, publishOptions)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundSnsContextConverter"/> class.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="serializer">The serializer used to serialize the message payload.</param>
    /// <param name="topicAttributeKey">The message attribute the Benzene topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    /// <param name="publishOptions">Optional FIFO/numeric-typing publish options.</param>
    public OutboundSnsContextConverter(string topicArn, ISerializer serializer, string topicAttributeKey = DefaultTopicAttribute, SnsPublishOptions? publishOptions = null)
    {
        _topicArn = topicArn;
        _serializer = serializer;
        _topicAttributeKey = topicAttributeKey;
        _publishOptions = publishOptions;
    }

    /// <summary>
    /// Builds an SNS publish request from the outbound context.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the SNS send message context.</returns>
    public Task<SnsSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>();
        foreach (var header in contextIn.Headers)
        {
            // Skip empty-valued headers: SNS rejects an empty message-attribute value and fails the
            // whole publish (mirrors the SQS converter and this package's documented contract).
            if (string.IsNullOrEmpty(header.Value))
            {
                continue;
            }

            messageAttributes[header.Key] = new MessageAttributeValue
            {
                StringValue = header.Value,
                DataType = SnsContextConverter<object>.DataTypeFor(_publishOptions, header.Value)
            };
        }

        // Carry the Benzene routing topic as a message attribute so a Benzene SNS Lambda consumer
        // routes to the right handler — mirroring SQS. Without it the round-trip resolves to a null
        // topic. Only when non-empty: SNS rejects an empty message-attribute value, and an empty
        // topic has no routing key to carry (unlike SQS, which accepts empty attribute values).
        if (!string.IsNullOrEmpty(contextIn.Topic))
        {
            messageAttributes[_topicAttributeKey] = new MessageAttributeValue { StringValue = contextIn.Topic, DataType = "String" };
        }

        SnsContextConverter<object>.GuardAttributeLimit(messageAttributes.Count);

        var publishRequest = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = _serializer.Serialize(contextIn.Request),
            MessageAttributes = messageAttributes
        };

        SnsContextConverter<object>.ApplyFifoProperties(publishRequest, contextIn.Headers, _publishOptions);

        return Task.FromResult(new SnsSendMessageContext(publishRequest));
    }

    /// <summary>
    /// Maps the SNS publish response's HTTP status code back onto the outbound context as an
    /// <see cref="IBenzeneResult{Void}"/>.
    /// </summary>
    /// <param name="contextIn">The outbound context to update with the result.</param>
    /// <param name="contextOut">The SNS send message context containing the response.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, SnsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}

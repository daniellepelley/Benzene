using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Converts a Benzene client request into an SNS <see cref="PublishRequest"/> and maps the SNS response
/// back onto the client context.
/// </summary>
/// <typeparam name="T">The message payload type being sent.</typeparam>
public class SnsContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, SnsSendMessageContext>
{
    /// <summary>
    /// The default message-attribute key the Benzene routing topic is written to. It is a single
    /// default, not a hard-coded value — pass a different key to interoperate with a consumer that
    /// routes on another attribute. Keep it in sync with the consumer's attribute key
    /// (<c>SnsMessageTopicGetter</c> reads <c>"topic"</c> by default).
    /// </summary>
    public const string DefaultTopicAttribute = "topic";

    private readonly ISerializer _serializer;
    private readonly string _topicArn;
    private readonly string _topicAttributeKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsContextConverter{T}"/> class, using the default JSON serializer.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="topicAttributeKey">The message attribute the Benzene topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public SnsContextConverter(string topicArn, string topicAttributeKey = DefaultTopicAttribute)
        :this( topicArn, new JsonSerializer(), topicAttributeKey)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsContextConverter{T}"/> class.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="serializer">The serializer used to serialize the message payload.</param>
    /// <param name="topicAttributeKey">The message attribute the Benzene topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public SnsContextConverter(string topicArn, ISerializer serializer, string topicAttributeKey = DefaultTopicAttribute)
    {
        _topicArn = topicArn;
        _serializer = serializer;
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Builds an SNS publish request from the client request.
    /// </summary>
    /// <param name="contextIn">The client context to convert.</param>
    /// <returns>A task that resolves to the SNS send message context.</returns>
    public Task<SnsSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>();
        foreach (var header in contextIn.Request.Headers)
        {
            messageAttributes[header.Key] = new MessageAttributeValue { StringValue = header.Value, DataType = "String" };
        }

        // Carry the Benzene routing topic as a message attribute so a Benzene SNS Lambda consumer
        // (SnsMessageTopicGetter reads this attribute) routes to the right handler — mirroring SQS.
        // Without it a Benzene→Benzene SNS round-trip resolves to a null topic and fails to route.
        // Only when non-empty: SNS rejects an empty message-attribute value ("must contain non-empty
        // message attribute value"), and an empty topic has no routing key to carry anyway (unlike
        // SQS, which accepts empty attribute values). See SnsContextConverterTest.
        if (!string.IsNullOrEmpty(contextIn.Request.Topic))
        {
            messageAttributes[_topicAttributeKey] = new MessageAttributeValue { StringValue = contextIn.Request.Topic, DataType = "String" };
        }

        return Task.FromResult(new SnsSendMessageContext(new PublishRequest
        {
            TopicArn = _topicArn,
            Message = _serializer.Serialize(contextIn.Request.Message),
            MessageAttributes = messageAttributes
        }));
    }

    /// <summary>
    /// Maps the SNS publish response's HTTP status code back onto the client context.
    /// </summary>
    /// <param name="contextIn">The client context to update with the result.</param>
    /// <param name="contextOut">The SNS send message context containing the response.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, SnsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}

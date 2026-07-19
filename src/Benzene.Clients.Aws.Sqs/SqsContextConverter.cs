using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Converts between a generic Benzene client context and an <see cref="SqsSendMessageContext"/>, so that
/// a Benzene client pipeline can send messages via SQS.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
public class SqsContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, SqsSendMessageContext>
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
    /// Initializes a new instance of the <see cref="SqsContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public SqsContextConverter(string queueUrl, string topicAttributeKey = DefaultTopicAttribute)
        :this(queueUrl, new JsonSerializer(), topicAttributeKey)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsContextConverter{T}"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    /// <param name="topicAttributeKey">The message attribute the topic is written to (defaults to <see cref="DefaultTopicAttribute"/>).</param>
    public SqsContextConverter(string queueUrl, ISerializer serializer, string topicAttributeKey = DefaultTopicAttribute)
    {
        _queueUrl = queueUrl;
        _serializer = serializer;
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Builds an SQS send message context, serializing the outgoing message as the message body and
    /// setting the topic as a message attribute.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="SqsSendMessageContext"/>.</returns>
    public Task<SqsSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>();
        foreach (var header in contextIn.Request.Headers)
        {
            messageAttributes[header.Key] = new MessageAttributeValue { StringValue = header.Value, DataType = "String" };
        }

        messageAttributes[_topicAttributeKey] = new MessageAttributeValue { StringValue = contextIn.Request.Topic, DataType = "String" };

        return Task.FromResult(new SqsSendMessageContext(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _serializer.Serialize(contextIn.Request.Message),
            MessageAttributes = messageAttributes
        }));
    }

    /// <summary>
    /// Maps the SQS send response's HTTP status code back onto the incoming Benzene client context.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="SqsSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, SqsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}

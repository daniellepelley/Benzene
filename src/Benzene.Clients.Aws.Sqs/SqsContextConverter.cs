using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

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
            // SQS rejects a message attribute whose value is empty, so skip empty headers rather than
            // fail the whole send (LocalStack tolerates them, but real SQS does not).
            if (!string.IsNullOrEmpty(header.Value))
            {
                messageAttributes[header.Key] = new MessageAttributeValue { StringValue = header.Value, DataType = "String" };
            }
        }

        // Only emit the routing-topic attribute for a non-empty topic: SQS rejects an empty attribute
        // value, and an empty topic carries no routing information anyway.
        if (!string.IsNullOrEmpty(contextIn.Request.Topic))
        {
            messageAttributes[_topicAttributeKey] = new MessageAttributeValue { StringValue = contextIn.Request.Topic, DataType = "String" };
        }

        GuardAttributeLimit(messageAttributes.Count);

        return Task.FromResult(new SqsSendMessageContext(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _serializer.Serialize(contextIn.Request.Message),
            MessageAttributes = messageAttributes
        }));
    }

    /// <summary>The maximum number of message attributes SQS accepts on a single message.</summary>
    internal const int MaxMessageAttributes = 10;

    internal static void GuardAttributeLimit(int attributeCount)
    {
        // SQS caps a message at 10 message attributes; the topic attribute counts toward it. Fail fast
        // with a clear message naming the count, rather than letting the SDK throw an opaque error
        // that the send path would otherwise swallow into a generic ServiceUnavailable.
        if (attributeCount > MaxMessageAttributes)
        {
            throw new InvalidOperationException(
                $"An SQS message can carry at most {MaxMessageAttributes} message attributes, but {attributeCount} were set " +
                "(the routing topic attribute counts toward the limit). Reduce the number of headers forwarded onto message attributes.");
        }
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

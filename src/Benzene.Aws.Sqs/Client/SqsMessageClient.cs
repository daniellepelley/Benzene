using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Benzene.Aws.Sqs.Client;

/// <summary>
/// Publishes messages to a single SQS queue, tagging each with a <c>topic</c> message attribute (and
/// optionally a <c>status</c> attribute).
/// </summary>
public class SqsMessageClient : ISqsClient
{
    /// <summary>
    /// The default message-attribute key the topic is written to. It is a single default, not a
    /// hard-coded value — pass a different key to
    /// <see cref="SqsMessageClient(IAmazonSQS, string, string)"/> to interoperate with a consumer that
    /// routes on another attribute. Keep it in sync with the consumer's attribute key.
    /// </summary>
    public const string DefaultTopicAttribute = "topic";

    private readonly IAmazonSQS _amazonSqs;
    private readonly string _queueUrl;
    private readonly string _topicAttributeKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsMessageClient"/> class.
    /// </summary>
    /// <param name="amazonSqs">The underlying SQS client.</param>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <param name="topicAttributeKey">
    /// The message attribute the topic is written to. Defaults to
    /// <see cref="DefaultTopicAttribute"/> (<c>"topic"</c>).
    /// </param>
    public SqsMessageClient(IAmazonSQS amazonSqs, string queueUrl, string topicAttributeKey = DefaultTopicAttribute)
    {
        _queueUrl = queueUrl;
        _amazonSqs = amazonSqs;
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Publishes a message to the queue.
    /// </summary>
    /// <param name="topic">The topic to tag the message with.</param>
    /// <param name="message">The message body.</param>
    /// <param name="status">An optional status to tag the message with. Omitted if null or empty.</param>
    /// <returns>A task that resolves to the HTTP status code of the send request, as a string.</returns>
    public async Task<string> PublishAsync(string topic, string message, string status)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = message,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    _topicAttributeKey, new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = topic
                    }
                }
            }
        };

        if (!string.IsNullOrEmpty(status))
        {
            request.MessageAttributes.Add("status",
                new MessageAttributeValue { DataType = "String", StringValue = status });
        }

        var response = await _amazonSqs.SendMessageAsync(request);
        return response.HttpStatusCode.ToString();
    }
}



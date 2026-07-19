using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// A health check that verifies connectivity to an SQS queue by sending a "ping" message to it.
/// </summary>
public class SqsHealthCheck : IHealthCheck
{
    private readonly IAmazonSQS _amazonSqs;
    private readonly string _queueUrl;
    private readonly string _topicAttributeKey;
    private const int TimeOut = 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsHealthCheck"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to ping.</param>
    /// <param name="amazonSqs">The SQS client used to send the ping message.</param>
    /// <param name="topicAttributeKey">
    /// The message attribute the ping topic is written to. Defaults to
    /// <see cref="OutboundSqsContextConverter.DefaultTopicAttribute"/> (<c>"topic"</c>) — pass the same
    /// key the queue's consumer routes on so the ping is routable there too.
    /// </param>
    public SqsHealthCheck(string queueUrl, IAmazonSQS amazonSqs, string topicAttributeKey = OutboundSqsContextConverter.DefaultTopicAttribute)
    {
        _queueUrl = queueUrl;
        _amazonSqs = amazonSqs;
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>
    /// Sends a "ping" message to the configured queue, failing if the send does not complete
    /// successfully within the timeout.
    /// </summary>
    /// <returns>A task that resolves to the outcome of the health check.</returns>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Queue", _queueUrl) };

        var delay = Task.Delay(TimeOut);
        var pingQueue = _amazonSqs.SendMessageAsync(new SendMessageRequest(_queueUrl, "{}")
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { _topicAttributeKey, new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = "ping"
                    }
                }
            }
        });

        await Task.WhenAny(delay, pingQueue);

        if (pingQueue.IsCompleted && pingQueue.Result.HttpStatusCode == HttpStatusCode.OK)
        {
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object>
                {
                    { "QueueUrl", _queueUrl },
                }, dependencies);
        }
        if (delay.IsCompleted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "QueueUrl", _queueUrl },
                    { "Error", $"Timed out, {TimeOut}ms" }
                }, dependencies);
        }
        return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            { "Error", $"Returned a status of {pingQueue.Result.HttpStatusCode}" },
            { "QueueUrl", _queueUrl }
        }, dependencies);
    }

    /// <summary>
    /// Gets the health check type identifier, <c>"Sqs"</c>.
    /// </summary>
    public string Type => "Sqs";
}

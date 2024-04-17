using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.Sqs;

public class SqsHealthCheck : IHealthCheck
{
    private readonly IAmazonSQS _amazonSqs;
    private readonly string _queueUrl;
    private const int TimeOut = 10000;

    public SqsHealthCheck(string queueUrl, IAmazonSQS amazonSqs)
    {
        _queueUrl = queueUrl;
        _amazonSqs = amazonSqs;
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var delay = Task.Delay(TimeOut);
        var pingQueue = _amazonSqs.SendMessageAsync(new SendMessageRequest(_queueUrl, "{}")
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "topic", new MessageAttributeValue
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
                });
        }
        if (delay.IsCompleted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "QueueUrl", _queueUrl },
                    { "Error", $"Timed out, {TimeOut}ms" }
                });
        }
        return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            { "Error", $"Returned a status of {pingQueue.Result.HttpStatusCode}" },
            { "QueueUrl", _queueUrl }
        });
    }

    public string Type => "Sqs";
}

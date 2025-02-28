using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Benzene.Aws.Sqs.Client;

public class SqsMessageClient : ISqsClient
{
    private readonly IAmazonSQS _amazonSqs;
    private readonly string _queueUrl;

    public SqsMessageClient(IAmazonSQS amazonSqs, string queueUrl)
    {
        _queueUrl = queueUrl;
        _amazonSqs = amazonSqs;
    }

    public async Task<string> PublishAsync(string topic, string message, string status)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = message,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    "topic", new MessageAttributeValue
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



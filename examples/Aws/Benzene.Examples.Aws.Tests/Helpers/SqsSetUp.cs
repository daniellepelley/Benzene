using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class SqsSetUp
{
    private static readonly string ServiceUrl;
    private static readonly string QueueUrl;

    static SqsSetUp()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var serviceUrl = configuration.GetValue<string>("AWS_SERVICE_URL");
        var queueUrl = configuration.GetValue<string>("MY_QUEUE_URL");

        ServiceUrl = string.IsNullOrEmpty(serviceUrl)
            ? "http://localhost:4566"
            : serviceUrl;
        QueueUrl = string.IsNullOrEmpty(queueUrl)
            ? "http://localhost:4566/245633934812/my-queue"
            : queueUrl;
    }

    public static async Task SetUp()
    {
        var client = CreateClient();
        await client.CreateQueueAsync("my-queue");
    }

    public static async Task TearDown()
    {
        await GetAllMessagesAsync();
        // var client = CreateClient();
        // await client.DeleteQueueAsync(QueueUrl);
    }

    public static async Task<Message[]> GetAllMessagesAsync()
    {
        var client = CreateClient();
        int count;

        var messages = new List<Message>();

        do
        {
            var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = QueueUrl,
                MessageAttributeNames = new[] { "All" }.ToList(),
                MaxNumberOfMessages = 10
            });

            count = result.Messages.Count;
            messages.AddRange(result.Messages);
        } while (count > 0);

        foreach (var message in messages)
        {
            await client.DeleteMessageAsync(QueueUrl, message.ReceiptHandle);
        }

        return messages.ToArray();
    }

    private static AmazonSQSClient CreateClient()
    {
        var client = new AmazonSQSClient(new AmazonSQSConfig
        {
            ServiceURL = ServiceUrl
        });
        return client;
    }
}
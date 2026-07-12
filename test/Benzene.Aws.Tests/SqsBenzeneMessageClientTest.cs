using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Tests.Fixtures;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class SqsBenzeneMessageClientTest : IClassFixture<SqsFixture>
{
    private const string ServiceUrl = "http://localhost:4566";
    private const string QueueName = "benzene-client-queue";
    private const string AccessKey = "123";
    private const string SecretKey = "xyz";

    private static AmazonSQSClient CreateAmazonSqsClient()
    {
        return new AmazonSQSClient(new BasicAWSCredentials(AccessKey, SecretKey), new AmazonSQSConfig
        {
            ServiceURL = ServiceUrl,
        });
    }

    [Fact]
    public async Task SendMessageAsync_RealQueue_DeliversMessage()
    {
        var amazonSqsClient = CreateAmazonSqsClient();
        var createQueueResponse = await amazonSqsClient.CreateQueueAsync(new CreateQueueRequest(QueueName));

        var client = new SqsBenzeneMessageClient(createQueueResponse.QueueUrl, amazonSqsClient,
            NullLogger<SqsBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);

        var received = await amazonSqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = createQueueResponse.QueueUrl,
            MaxNumberOfMessages = 10
        });

        Assert.Contains(received.Messages, m => m.Body.Contains("some-message"));
    }
}

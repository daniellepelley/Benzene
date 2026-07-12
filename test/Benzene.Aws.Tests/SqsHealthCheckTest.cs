using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Tests.Fixtures;
using Benzene.Clients.Aws.Sqs;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class SqsHealthCheckTest : IClassFixture<SqsFixture>
{
    private const string ServiceUrl = "http://localhost:4566";
    private const string QueueName = "benzene-health-check-queue";
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
    public async Task ExecuteAsync_RealQueue_ReturnsHealthy()
    {
        var amazonSqsClient = CreateAmazonSqsClient();
        var createQueueResponse = await amazonSqsClient.CreateQueueAsync(new CreateQueueRequest(QueueName));

        var healthCheck = new SqsHealthCheck(createQueueResponse.QueueUrl, amazonSqsClient);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
    }
}

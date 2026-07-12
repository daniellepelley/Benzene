using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Tests.Fixtures;
using Benzene.Clients;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Middleware;
using Benzene.Results;
using Moq;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class SnsBenzeneMessageClientTest : IClassFixture<SqsFixture>
{
    private const string ServiceUrl = "http://localhost:4566";
    private const string AccessKey = "123";
    private const string SecretKey = "xyz";

    private static IAmazonSimpleNotificationService CreateAmazonSnsClient()
    {
        return new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials(AccessKey, SecretKey), new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = ServiceUrl,
        });
    }

    [Fact]
    public async Task SendMessageAsync_RealTopic_ReturnsAccepted()
    {
        var amazonSnsClient = CreateAmazonSnsClient();
        var createTopicResponse = await amazonSnsClient.CreateTopicAsync(new CreateTopicRequest("benzene-client-topic"));

        var client = new SnsBenzeneMessageClient(createTopicResponse.TopicArn, amazonSnsClient,
            Mock.Of<IBenzeneLogger>(), new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Clients.Aws.Sqs;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.Sqs;

public class SqsHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_OkResponse_ReturnsHealthy()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new SqsHealthCheck("some-queue-url", mockSqsClient.Object);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Sqs", healthCheck.Type);
    }

    [Fact]
    public async Task ExecuteAsync_NonOkResponse_ReturnsUnhealthy()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        var healthCheck = new SqsHealthCheck("some-queue-url", mockSqsClient.Object);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }
}

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
    public async Task Reachability_OkResponse_ReturnsHealthy_NonDestructively()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new SqsHealthCheck("some-queue-url", mockSqsClient.Object);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Sqs", healthCheck.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Queue", dependency.Kind);
        Assert.Equal("some-queue-url", dependency.Name);
        // The default probe is read-only — it must NOT send a message.
        mockSqsClient.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reachability_NonOkResponse_ReturnsUnhealthy()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        var result = await new SqsHealthCheck("some-queue-url", mockSqsClient.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Reachability_Faults_ReturnsUnhealthy_AndKeepsTheQueueDependency()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("connection refused"));

        var result = await new SqsHealthCheck("some-queue-url", mockSqsClient.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        // The structured result (incl. the Queue dependency and the failure TYPE, never the message) survives the fault.
        Assert.Equal("AmazonSQSException", result.Data["Error"]);
        Assert.Equal("some-queue-url", Assert.Single(result.Dependencies).Name);
    }

    [Fact]
    public async Task Reachability_PermissionDenied_IsPersistentFailure_AndSurfacesTheDiscriminators()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("access denied for arn:...")
            {
                ErrorCode = "AccessDenied",
                StatusCode = HttpStatusCode.Forbidden
            });

        var result = await new SqsHealthCheck("some-queue-url", mockSqsClient.Object).ExecuteAsync();

        // "I can't probe this" (403) is a Warning, not a Failed - a least-privilege caller stays green (§3.9).
        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.True(result.IsPersistent);
        Assert.Equal("AccessDenied", result.Data["ErrorCode"]);
        Assert.Equal(403, result.Data["StatusCode"]);
        Assert.Equal("AmazonSQSException", result.Data["Error"]);
    }

    [Fact]
    public async Task Active_SendsAPing_AndReportsUnderTheActiveType()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new SqsHealthCheck("some-queue-url", mockSqsClient.Object, HealthCheckMode.Active);

        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Sqs.Active", healthCheck.Type);
        mockSqsClient.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

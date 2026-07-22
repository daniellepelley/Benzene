using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Benzene.Clients.Aws.EventBridge;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.EventBridge;

public class EventBridgeHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_OkResponse_ReturnsHealthy_NonDestructively()
    {
        var mock = new Mock<IAmazonEventBridge>();
        mock.Setup(x => x.DescribeEventBusAsync(It.IsAny<DescribeEventBusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeEventBusResponse { HttpStatusCode = HttpStatusCode.OK });

        var healthCheck = new EventBridgeHealthCheck(mock.Object);
        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("EventBridge", healthCheck.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("EventBus", dependency.Kind);
        Assert.Equal("default", dependency.Name);
        // The default probe is read-only - it must NOT publish an event.
        mock.Verify(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NamedBus_CarriesThatBusAsTheDependency()
    {
        var mock = new Mock<IAmazonEventBridge>();
        mock.Setup(x => x.DescribeEventBusAsync(It.IsAny<DescribeEventBusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeEventBusResponse { HttpStatusCode = HttpStatusCode.OK });

        var result = await new EventBridgeHealthCheck(mock.Object, "orders-bus").ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("orders-bus", Assert.Single(result.Dependencies).Name);
    }

    [Fact]
    public async Task ExecuteAsync_ClientThrows_ReturnsUnhealthy_WithTheBusDependency()
    {
        var mock = new Mock<IAmazonEventBridge>();
        mock.Setup(x => x.DescribeEventBusAsync(It.IsAny<DescribeEventBusRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonEventBridgeException("connection refused"));

        var result = await new EventBridgeHealthCheck(mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("AmazonEventBridgeException", result.Data["Error"]);
        Assert.Equal("default", Assert.Single(result.Dependencies).Name);
    }

    [Fact]
    public async Task ExecuteAsync_PermissionDenied_DegradesToWarning_AndSurfacesTheDiscriminators()
    {
        var mock = new Mock<IAmazonEventBridge>();
        mock.Setup(x => x.DescribeEventBusAsync(It.IsAny<DescribeEventBusRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonEventBridgeException("not authorized")
            {
                ErrorCode = "AccessDeniedException",
                StatusCode = HttpStatusCode.Forbidden
            });

        var result = await new EventBridgeHealthCheck(mock.Object).ExecuteAsync();

        // Lacking events:DescribeEventBus is a Warning, not a failure (§3.9).
        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Equal("AccessDeniedException", result.Data["ErrorCode"]);
        Assert.Equal(403, result.Data["StatusCode"]);
    }
}

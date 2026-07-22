using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Benzene.Clients.Azure.QueueStorage;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.Clients.Azure.QueueStorage;

public class QueueStorageHealthCheckTest
{
    private static Mock<QueueClient> QueueClientMock(string name = "orders")
    {
        var mock = new Mock<QueueClient>();
        mock.Setup(x => x.Name).Returns(name);
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_Reachable_ReturnsHealthy_NonDestructively()
    {
        var mock = QueueClientMock();
        // The check discards the response (Azure throws on non-success), so a null return is a healthy round-trip.
        mock.Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Response<QueueProperties>)null);

        var healthCheck = new QueueStorageHealthCheck(mock.Object);
        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("QueueStorage", healthCheck.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Queue", dependency.Kind);
        Assert.Equal("orders", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Faults_ReturnsUnhealthy_WithTheQueueDependency_AndErrorCode()
    {
        var mock = QueueClientMock();
        mock.Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "boom", "InternalError", null));

        var result = await new QueueStorageHealthCheck(mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("orders", Assert.Single(result.Dependencies).Name);
        Assert.Equal("InternalError", result.Data["ErrorCode"]);
        Assert.Equal(500, result.Data["StatusCode"]);
    }

    [Fact]
    public async Task ExecuteAsync_PermissionDenied_DegradesToWarning_AndSurfacesTheDiscriminators()
    {
        var mock = QueueClientMock();
        mock.Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(403, "no access", "AuthorizationFailure", null));

        var result = await new QueueStorageHealthCheck(mock.Object).ExecuteAsync();

        // Lacking read permission on the queue is a Warning, not a failure (§3.9).
        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Equal("AuthorizationFailure", result.Data["ErrorCode"]);
        Assert.Equal(403, result.Data["StatusCode"]);
    }
}

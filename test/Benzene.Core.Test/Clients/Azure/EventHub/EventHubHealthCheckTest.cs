using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Clients.Azure.EventHub;
using Benzene.HealthChecks.Core;
using Moq;
using Xunit;

namespace Benzene.Test.Clients.Azure.EventHub;

public class EventHubHealthCheckTest
{
    private const string Hub = "orders-hub";

    // EventHubProducerClient.EventHubName is non-virtual, so it is passed to the check explicitly rather
    // than mocked; only the (virtual) GetEventHubPropertiesAsync is substituted here.
    private static Mock<EventHubProducerClient> ProducerMock() => new();

    [Fact]
    public async Task ExecuteAsync_Reachable_ReturnsHealthy_NonDestructively()
    {
        var mock = ProducerMock();
        // The check discards the response (the SDK throws on failure), so a null return is a healthy round-trip.
        mock.Setup(x => x.GetEventHubPropertiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventHubProperties)null);

        var healthCheck = new EventHubHealthCheck(mock.Object, Hub);
        var result = await healthCheck.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("EventHub", healthCheck.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("EventHub", dependency.Kind);
        Assert.Equal("orders-hub", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Faults_ReturnsUnhealthy_WithTheHubDependency_AndReason()
    {
        var mock = ProducerMock();
        mock.Setup(x => x.GetEventHubPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EventHubsException(false, "orders-hub", "boom", EventHubsException.FailureReason.GeneralError));

        var result = await new EventHubHealthCheck(mock.Object, Hub).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("orders-hub", Assert.Single(result.Dependencies).Name);
        Assert.Equal("GeneralError", result.Data["ErrorCode"]);
    }

    [Fact]
    public async Task ExecuteAsync_Unauthorized_DegradesToWarning_AndMapsTo403()
    {
        var mock = ProducerMock();
        mock.Setup(x => x.GetEventHubPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("bad token"));

        var result = await new EventHubHealthCheck(mock.Object, Hub).ExecuteAsync();

        // Event Hubs has no HTTP status; a bad credential surfaces as UnauthorizedAccessException, mapped
        // to 403 so the shared policy degrades it to Warning (§3.9).
        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Equal("Unauthorized", result.Data["ErrorCode"]);
        Assert.Equal(403, result.Data["StatusCode"]);
    }
}

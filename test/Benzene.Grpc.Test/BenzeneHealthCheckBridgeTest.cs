using Benzene.Grpc.AspNet;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using BenzeneHealthCheckResult = Benzene.HealthChecks.Core.HealthCheckResult;
using BenzeneHealthCheckStatus = Benzene.HealthChecks.Core.HealthCheckStatus;
using IBenzeneHealthCheck = Benzene.HealthChecks.Core.IHealthCheck;
using IBenzeneHealthCheckResult = Benzene.HealthChecks.Core.IHealthCheckResult;

namespace Benzene.Grpc.Test;

public class BenzeneHealthCheckBridgeTest
{
    [Fact]
    public async Task CheckHealthAsync_WhenNoChecksAreRegistered_ReturnsHealthy()
    {
        var bridge = new BenzeneHealthCheckBridge(Array.Empty<IBenzeneHealthCheck>());

        var result = await bridge.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAllChecksPass_ReturnsHealthy()
    {
        var bridge = new BenzeneHealthCheckBridge(new[] { new FakeHealthCheck("a", BenzeneHealthCheckStatus.Ok) });

        var result = await bridge.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAnyCheckFails_ReturnsUnhealthy()
    {
        var bridge = new BenzeneHealthCheckBridge(new[]
        {
            new FakeHealthCheck("a", BenzeneHealthCheckStatus.Ok),
            new FakeHealthCheck("b", BenzeneHealthCheckStatus.Failed),
        });

        var result = await bridge.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoneFailButOneWarns_ReturnsDegraded()
    {
        var bridge = new BenzeneHealthCheckBridge(new[]
        {
            new FakeHealthCheck("a", BenzeneHealthCheckStatus.Ok),
            new FakeHealthCheck("b", BenzeneHealthCheckStatus.Warning),
        });

        var result = await bridge.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    private class FakeHealthCheck : IBenzeneHealthCheck
    {
        private readonly string _status;

        public FakeHealthCheck(string type, string status)
        {
            Type = type;
            _status = status;
        }

        public string Type { get; }

        public Task<IBenzeneHealthCheckResult> ExecuteAsync()
        {
            return Task.FromResult<IBenzeneHealthCheckResult>(new BenzeneHealthCheckResult(_status, Type, new Dictionary<string, object>()));
        }
    }
}

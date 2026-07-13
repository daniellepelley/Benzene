using Microsoft.Extensions.Diagnostics.HealthChecks;
using BenzeneHealthCheckStatus = Benzene.HealthChecks.Core.HealthCheckStatus;
using IBenzeneHealthCheck = Benzene.HealthChecks.Core.IHealthCheck;

namespace Benzene.Grpc.AspNet;

/// <summary>
/// Bridges Benzene's own <see cref="IBenzeneHealthCheck"/>s onto ASP.NET Core's health check system, so
/// <c>Grpc.AspNetCore.HealthChecks</c> can surface them over grpc.health.v1's <c>Check</c>/<c>Watch</c>.
/// Registered via <c>services.AddGrpcHealthChecks().AddCheck&lt;BenzeneHealthCheckBridge&gt;("benzene")</c>
/// when <see cref="BenzeneGrpcOptions.EnableHealthChecks"/> is set. Every Benzene check registered in the
/// ASP.NET Core container is executed; the aggregate is unhealthy if any failed, degraded if any warned,
/// healthy otherwise.
/// </summary>
public class BenzeneHealthCheckBridge : IHealthCheck
{
    private readonly IEnumerable<IBenzeneHealthCheck> _healthChecks;

    public BenzeneHealthCheckBridge(IEnumerable<IBenzeneHealthCheck> healthChecks)
    {
        _healthChecks = healthChecks;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = _healthChecks.ToArray();
        if (checks.Length == 0)
        {
            return HealthCheckResult.Healthy("No Benzene health checks are registered.");
        }

        var results = await Task.WhenAll(checks.Select(x => x.ExecuteAsync()));

        var data = new Dictionary<string, object>();
        foreach (var result in results)
        {
            data[result.Type] = result.Status;
        }

        if (results.Any(x => x.Status == BenzeneHealthCheckStatus.Failed))
        {
            return HealthCheckResult.Unhealthy("One or more Benzene health checks failed.", data: data);
        }

        if (results.Any(x => x.Status == BenzeneHealthCheckStatus.Warning))
        {
            return HealthCheckResult.Degraded("One or more Benzene health checks reported a warning.", data: data);
        }

        return HealthCheckResult.Healthy("All Benzene health checks passed.", data: data);
    }
}

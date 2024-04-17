namespace Benzene.HealthChecks.Core;

public class HealthCheckResponse : IHealthCheckResponse<HealthCheckResult>
{
    public HealthCheckResponse(bool isHealthy, IDictionary<string, HealthCheckResult> healthChecks)
    {
        HealthChecks = healthChecks;
        IsHealthy = isHealthy;
    }

    public bool IsHealthy { get; }
    public IDictionary<string, HealthCheckResult> HealthChecks { get; }
}

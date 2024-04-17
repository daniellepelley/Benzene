namespace Benzene.HealthChecks.Core;

public interface IHealthCheckResponse<THealthCheckResult> where THealthCheckResult : IHealthCheckResult
{
    bool IsHealthy { get; }
    IDictionary<string, THealthCheckResult> HealthChecks { get; }
}

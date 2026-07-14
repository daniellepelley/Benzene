namespace Benzene.HealthChecks.Core;

/// <summary>Default <see cref="IHealthCheckResponse{THealthCheckResult}"/> implementation, closing the generic over <see cref="HealthCheckResult"/>.</summary>
public class HealthCheckResponse : IHealthCheckResponse<HealthCheckResult>
{
    /// <summary>Initializes a new instance of the <see cref="HealthCheckResponse"/> class.</summary>
    /// <param name="isHealthy">Whether the aggregated set of checks is considered healthy.</param>
    /// <param name="healthChecks">Every check's result, keyed by its identifier.</param>
    public HealthCheckResponse(bool isHealthy, IDictionary<string, HealthCheckResult> healthChecks)
    {
        HealthChecks = healthChecks;
        IsHealthy = isHealthy;
    }

    /// <inheritdoc />
    public bool IsHealthy { get; }

    /// <inheritdoc />
    public IDictionary<string, HealthCheckResult> HealthChecks { get; }
}

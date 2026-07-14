namespace Benzene.HealthChecks.Core;

/// <summary>The aggregated outcome of running every registered <see cref="IHealthCheck"/>.</summary>
/// <typeparam name="THealthCheckResult">The concrete result type each individual check reported.</typeparam>
public interface IHealthCheckResponse<THealthCheckResult> where THealthCheckResult : IHealthCheckResult
{
    /// <summary>
    /// <c>true</c> unless at least one check reported <see cref="HealthCheckStatus.Failed"/> - a
    /// <see cref="HealthCheckStatus.Warning"/> result does not flip this to <c>false</c> (see
    /// <c>Benzene.HealthChecks.HealthCheckProcessor.PerformHealthChecksAsync</c> for the exact rule).
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>Every check's result, keyed by its <see cref="IHealthCheck.Type"/>.</summary>
    IDictionary<string, THealthCheckResult> HealthChecks { get; }
}

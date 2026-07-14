namespace Benzene.HealthChecks.Core;

/// <summary>
/// A single health check: verifies one dependency or condition (e.g. database connectivity, a
/// downstream HTTP service) and reports the outcome. Implementations are typically registered via
/// <see cref="IHealthCheckBuilder"/> and run together by whatever health-check middleware/endpoint
/// aggregates their results into an <see cref="IHealthCheckResponse{THealthCheckResult}"/>.
/// </summary>
public interface IHealthCheck
{
    /// <summary>A short identifier for this check, used as its key in the aggregated response (e.g. "Database", "HttpPing").</summary>
    string Type { get; }

    /// <summary>
    /// Runs the check and returns its outcome. Should not throw for expected failure conditions
    /// (e.g. connection refused) - report them via a failed <see cref="IHealthCheckResult"/> instead.
    /// </summary>
    Task<IHealthCheckResult> ExecuteAsync();
}


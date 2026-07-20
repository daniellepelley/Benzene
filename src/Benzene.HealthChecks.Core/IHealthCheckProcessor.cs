using Benzene.Abstractions.Results;

namespace Benzene.HealthChecks.Core;

/// <summary>
/// Runs a set of <see cref="IHealthCheck"/>s and aggregates their outcomes into a single result.
/// Injectable so the timeout (and, in future, tags/subset execution) can be configured without a
/// breaking change to the execution engine.
/// </summary>
public interface IHealthCheckProcessor
{
    /// <summary>
    /// Runs every check (each under a timeout and exception isolation), captures each check's
    /// duration, and returns an aggregated <c>HealthCheckResponse</c> mapped to
    /// <see cref="BenzeneResultStatus.Ok"/> (HTTP 200) when healthy or
    /// <see cref="BenzeneResultStatus.ServiceUnavailable"/> (HTTP 503) when not.
    /// </summary>
    Task<IBenzeneResult> PerformHealthChecksAsync(IHealthCheck[] healthChecks);
}

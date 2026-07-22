using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>Discovers the set of <see cref="IHealthCheck"/>s that have been registered with the dependency container.</summary>
public interface IHealthCheckFinder
{
    /// <summary>
    /// Returns the checks registered as plain <see cref="IHealthCheck"/> (process-local self-checks and any
    /// the developer registered directly). Does <b>not</b> include the dependency-category checks - this is
    /// the probe-safe set (§3.2).
    /// </summary>
    /// <returns>The registered health checks, in no particular guaranteed order.</returns>
    IHealthCheck[] FindHealthChecks();

    /// <summary>
    /// Returns the dependency-category checks (<see cref="IDependencyHealthCheck"/> - auto-wired external
    /// dependency checks), de-duplicated by <see cref="IDependencyHealthCheck.DedupKey"/> so two
    /// registrations of the same dependency collapse to one. Harvested by the general <c>healthcheck</c>
    /// probe only, never by liveness/readiness/contracts. Defaults to empty so existing finders stay
    /// source-compatible.
    /// </summary>
    /// <returns>The de-duplicated dependency-category health checks.</returns>
    IDependencyHealthCheck[] FindDependencyHealthChecks() => Array.Empty<IDependencyHealthCheck>();
}

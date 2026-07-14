using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>Discovers the set of <see cref="IHealthCheck"/>s that have been registered with the dependency container.</summary>
public interface IHealthCheckFinder
{
    /// <summary>Returns every registered health check.</summary>
    /// <returns>The registered health checks, in no particular guaranteed order.</returns>
    IHealthCheck[] FindHealthChecks();
}

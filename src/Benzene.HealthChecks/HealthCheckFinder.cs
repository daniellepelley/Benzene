using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Default <see cref="IHealthCheckFinder"/> implementation. Relies on the dependency injection container
/// to supply every <see cref="IHealthCheck"/> registered against that interface (e.g. via
/// <see cref="HealthCheckBuilder.AddHealthCheck{THealthCheck}"/>).
/// </summary>
public class HealthCheckFinder : IHealthCheckFinder
{
    private readonly IHealthCheck[] _healthChecks;
    private readonly IDependencyHealthCheck[] _dependencyHealthChecks;

    /// <summary>Initializes a new instance of the <see cref="HealthCheckFinder"/> class.</summary>
    /// <param name="healthChecks">
    /// The checks registered as plain <see cref="IHealthCheck"/> (the probe-safe set). In a Microsoft
    /// DI container, resolving <c>IEnumerable&lt;IHealthCheck&gt;</c> does not include services registered
    /// only under <see cref="IDependencyHealthCheck"/>, so the two sets stay disjoint.
    /// </param>
    /// <param name="dependencyHealthChecks">The checks registered under the dependency category.</param>
    public HealthCheckFinder(IEnumerable<IHealthCheck> healthChecks, IEnumerable<IDependencyHealthCheck> dependencyHealthChecks)
    {
        _healthChecks = healthChecks.ToArray();
        // De-dup by key so two registrations of the same dependency collapse to one check (§3.1).
        _dependencyHealthChecks = dependencyHealthChecks
            .GroupBy(x => x.DedupKey)
            .Select(g => g.First())
            .ToArray();
    }

    /// <inheritdoc />
    public IHealthCheck[] FindHealthChecks()
    {
        return _healthChecks;
    }

    /// <inheritdoc />
    public IDependencyHealthCheck[] FindDependencyHealthChecks()
    {
        return _dependencyHealthChecks;
    }
}

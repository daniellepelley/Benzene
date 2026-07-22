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
    private readonly IReadinessHealthCheck[] _readinessHealthChecks;

    /// <summary>Initializes a new instance of the <see cref="HealthCheckFinder"/> class.</summary>
    /// <param name="healthChecks">
    /// The checks registered as plain <see cref="IHealthCheck"/> (the liveness-safe set). In a Microsoft
    /// DI container, resolving <c>IEnumerable&lt;IHealthCheck&gt;</c> does not include services registered
    /// only under <see cref="IReadinessHealthCheck"/>, so the two sets stay disjoint.
    /// </param>
    /// <param name="readinessHealthChecks">The checks registered under the readiness category.</param>
    public HealthCheckFinder(IEnumerable<IHealthCheck> healthChecks, IEnumerable<IReadinessHealthCheck> readinessHealthChecks)
    {
        _healthChecks = healthChecks.ToArray();
        // De-dup by key so two registrations of the same dependency collapse to one check (§3.1).
        _readinessHealthChecks = readinessHealthChecks
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
    public IReadinessHealthCheck[] FindReadinessHealthChecks()
    {
        return _readinessHealthChecks;
    }
}

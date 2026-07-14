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

    /// <summary>Initializes a new instance of the <see cref="HealthCheckFinder"/> class.</summary>
    /// <param name="healthChecks">The health checks resolved from the container.</param>
    public HealthCheckFinder(IEnumerable<IHealthCheck> healthChecks)
    {
        _healthChecks = healthChecks.ToArray();
    }

    /// <inheritdoc />
    public IHealthCheck[] FindHealthChecks()
    {
        return _healthChecks;
    }
}

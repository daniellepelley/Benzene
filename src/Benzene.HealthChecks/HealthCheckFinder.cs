using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public class HealthCheckFinder : IHealthCheckFinder
{
    private readonly IHealthCheck[] _healthChecks;

    public HealthCheckFinder(IEnumerable<IHealthCheck> healthChecks)
    {
        _healthChecks = healthChecks.ToArray();
    }
    
    public IHealthCheck[] FindHealthChecks()
    {
        return _healthChecks;
    }
}

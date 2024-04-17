using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public interface IHealthCheckFinder
{
    IHealthCheck[] FindHealthChecks();
}

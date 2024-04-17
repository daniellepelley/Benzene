using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.HealthChecks.Core;

namespace Benzene.Cache.Core;

public class CacheHealthCheckFactory<TCacheService> : IHealthCheckFactory where TCacheService : class, ICacheService
{
    public IHealthCheck Create(IServiceResolver resolver)
    {
        return new CacheHealthCheck<TCacheService>(resolver.GetService<TCacheService>(), resolver.GetService<IBenzeneLogger>());
    }
}

using Benzene.HealthChecks.Core;

namespace Benzene.Cache.Core;

public static class Extensions
{
    public static IHealthCheckBuilder AddCacheHealthCheck<TCacheService>(this IHealthCheckBuilder builder) where TCacheService : class, ICacheService
    {
        return builder.AddHealthCheckFactory(new CacheHealthCheckFactory<TCacheService>());
    }
}

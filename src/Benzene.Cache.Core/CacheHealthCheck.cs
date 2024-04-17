using Benzene.Abstractions.Logging;
using Benzene.HealthChecks.Core;

namespace Benzene.Cache.Core;

public class CacheHealthCheck<TCacheService> : IHealthCheck where TCacheService : ICacheService
{
    private readonly IBenzeneLogger _logger;
    private readonly TCacheService _cacheService;

    public string Type => "Cache";

    public CacheHealthCheck(TCacheService cacheService, IBenzeneLogger logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        try
        {
            var canConnect = await _cacheService.CanConnectAsync();

            return HealthCheckResult.CreateInstance(canConnect, Type, new Dictionary<string, object>
            {
                { "CanConnect", canConnect },
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in cache health check");
            return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
            {
                { "CanConnect", false },
                { "Error", ex.Message }
            });
        }
    }
}

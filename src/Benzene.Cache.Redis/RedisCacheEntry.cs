using Benzene.Abstractions.Logging;
using Benzene.Cache.Core;
using Benzene.Diagnostics.Timers;

namespace Benzene.Cache.Redis;

internal class RedisCacheEntry<T> : CacheEntry<T>
{
    private readonly RedisCacheService _service;
    private readonly string _key;

    public RedisCacheEntry(RedisCacheService redisCacheService, string key)
    {
        _service = redisCacheService;
        _key = key;
    }

    protected override IBenzeneLogger Logger => _service.Logger;

    protected override IProcessTimerFactory ProcessTimerFactory => _service.ProcessTimerFactory;

    protected override string KeyDescription => _key;


    protected override async Task<string?> GetEntryValueAsync()
    {
        try
        {
            var redisDatabase = await _service.RedisSetup();
            return await redisDatabase.StringGetAsync(_key);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error getting value from cache");
            return "";
        }
    }

    protected override async Task<bool> SetEntryValueAsync(string value, TimeSpan? expireIn)
    {
        try
        {
            var redisDatabase = await _service.RedisSetup();
            return await redisDatabase.StringSetAsync(_key, value, expireIn ?? _service.DefaultCacheLifespan);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error setting value in cache");
            return false;
        }
    }

    protected override async Task<bool> InvalidateEntryAsync()
    {
        try
        {
            var redisDatabase = await _service.RedisSetup();
            return await redisDatabase.KeyDeleteAsync(_key);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error deleting key from cache");
            return false;
        }
    }
}

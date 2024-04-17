using Benzene.Abstractions.Logging;
using Benzene.Cache.Core;
using Benzene.Diagnostics.Timers;

namespace Benzene.Cache.Redis;

internal class RedisMultiKeyActions<T> : CacheWriteActions<T>
{
    private readonly RedisCacheService _service;
    private readonly string[] _keys;

    public RedisMultiKeyActions(RedisCacheService redisCacheService, IEnumerable<string> keys)
    {
        _service = redisCacheService;
        _keys = keys.ToArray();
    }

    protected override IBenzeneLogger Logger => _service.Logger;

    protected override IProcessTimerFactory ProcessTimerFactory => _service.ProcessTimerFactory;

    protected override string KeyDescription => string.Join(", ", _keys);

    protected override async Task<bool> InvalidateEntryAsync()
    {
        long deletedKeys = 0;
        try
        {
            var redisDatabase = await _service.RedisSetup();
            foreach (var key in _keys)
            {
                if (await redisDatabase.KeyDeleteAsync(key))
                {
                    deletedKeys++;
                }

            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error deleting keys from cache");
        }
        return deletedKeys > 0;
    }

    protected override async Task<bool> SetEntryValueAsync(string value, TimeSpan? expireIn)
    {
        long updatedKeys = 0;
        try
        {
            var redisDatabase = await _service.RedisSetup();
            foreach (var key in _keys)
            {
                if (await redisDatabase.StringSetAsync(key, value, expireIn ?? _service.DefaultCacheLifespan))
                {
                    updatedKeys++;
                }

            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error setting value in cache");
        }
        return updatedKeys > 0;
    }
}

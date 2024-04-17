using Benzene.Abstractions.Logging;
using Benzene.Cache.Core;
using Benzene.Diagnostics.Timers;
using StackExchange.Redis;

namespace Benzene.Cache.Redis;

internal class RedisWildcardActions : CacheInvalidateActions
{
    private const int MaxKeyForDelete = 1048000;

    private readonly RedisCacheService _service;
    private readonly string _pattern;

    public RedisWildcardActions(RedisCacheService redisCacheService, string pattern)
    {
        _service = redisCacheService;
        _pattern = pattern;
    }

    protected override IBenzeneLogger Logger => _service.Logger;

    protected override IProcessTimerFactory ProcessTimerFactory => _service.ProcessTimerFactory;

    protected override string KeyDescription => _pattern;

    protected override async Task<bool> InvalidateEntryAsync()
    {
        long deletedKeys = 0;
        try
        {
            var redisDatabase = await _service.RedisSetup();
            Logger.LogDebug("Sending {pattern} search to cache", _pattern);
            var result = (RedisKey[]?)await redisDatabase.ExecuteAsync("KEYS", _pattern);
            Logger.LogDebug("Result for {pattern} - {result.Length} keys.", _pattern, result?.Length);
            for (var i = 0; i < result?.Length; i += MaxKeyForDelete)
            {
                var keysForSending = result.Skip(i).Take(MaxKeyForDelete).ToArray();
                Logger.LogDebug("Deleting batch of {keysForSending.Length} keys.", keysForSending.Length);
                deletedKeys += await redisDatabase.KeyDeleteAsync(keysForSending);
            }
            Logger.LogDebug("Deleted {deletedKeys} keys.", deletedKeys);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error deleting keys from cache");
        }
        return deletedKeys > 0;
    }
}

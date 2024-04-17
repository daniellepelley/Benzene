using Benzene.Abstractions.Logging;
using Benzene.Cache.Core;
using Benzene.Diagnostics.Timers;
using StackExchange.Redis;

namespace Benzene.Cache.Redis;

public abstract class RedisCacheService : ICacheService
{
    public IBenzeneLogger Logger { get; }
    public IProcessTimerFactory ProcessTimerFactory { get; }

    private readonly Lazy<Task<IConnectionMultiplexer>> _redisConnection;

    public virtual TimeSpan DefaultCacheLifespan => TimeSpan.FromMinutes(5);

    protected RedisCacheService(IBenzeneLogger logger, IProcessTimerFactory processTimerFactory, IRedisConnectionFactory connectionFactory)
    {
        Logger = logger;
        ProcessTimerFactory = processTimerFactory;
        _redisConnection = new Lazy<Task<IConnectionMultiplexer>>(() => Task.Run(async () =>
        {
            using var scope = processTimerFactory.Create("RedisCacheService_Connect");
            var options = await GetConfigurationOptionsAsync() ?? throw new InvalidOperationException("Redis configuration options are not set");
            return await connectionFactory.ConnectAsync(options);
        }));
    }

    protected abstract Task<ConfigurationOptions> GetConfigurationOptionsAsync();

    protected void StartConnection()
    {
        _ = _redisConnection.Value;
    }

    internal async Task<IDatabase> RedisSetup()
    {
        var multiplexer = await _redisConnection.Value;
        return multiplexer.GetDatabase();
    }

    public async Task<bool> CanConnectAsync()
    {
        var redisDatabase = await RedisSetup();
        await redisDatabase.PingAsync();
        return true;
    }

    protected ICacheEntry<T> CreateCacheEntry<T>(string key)
    {
        return new RedisCacheEntry<T>(this, key);
    }

    protected ICacheWriteActions<T> CreateMultiKeyActions<T>(IEnumerable<string> keys)
    {
        return new RedisMultiKeyActions<T>(this, keys);
    }

    protected ICacheInvalidateActions CreatePrefixActions(string prefix)
    {
        return new RedisWildcardActions(this, prefix + "*");
    }

    protected ICacheInvalidateActions CreateWildcardActions(string pattern)
    {
        return new RedisWildcardActions(this, pattern);
    }
}

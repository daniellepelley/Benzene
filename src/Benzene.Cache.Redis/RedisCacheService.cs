using System.Text;
using Microsoft.Extensions.Logging;
using Benzene.Cache.Core;
using Benzene.Diagnostics.Timers;
using StackExchange.Redis;

namespace Benzene.Cache.Redis;

public abstract class RedisCacheService : ICacheService
{
    public ILogger Logger { get; }
    public IProcessTimerFactory ProcessTimerFactory { get; }

    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly object _connectionLock = new();
    private Task<IConnectionMultiplexer>? _redisConnectionTask;

    public virtual TimeSpan DefaultCacheLifespan => TimeSpan.FromMinutes(5);

    protected RedisCacheService(ILogger<RedisCacheService> logger, IProcessTimerFactory processTimerFactory, IRedisConnectionFactory connectionFactory)
    {
        Logger = logger;
        ProcessTimerFactory = processTimerFactory;
        _connectionFactory = connectionFactory;
    }

    protected abstract Task<ConfigurationOptions> GetConfigurationOptionsAsync();

    // Returns the shared connect task, (re)starting it if we've never connected or the previous
    // attempt faulted/was cancelled. A Lazy<Task<T>> would memoize the FIRST task object, so a
    // single connection blip at startup (Redis and the app coming up together, AbortOnConnectFail
    // default true) cached a faulted task for the process lifetime - the cache then stayed bypassed
    // and the health check red forever, even after Redis recovered. A successful multiplexer is kept
    // (StackExchange.Redis reconnects internally); only a failed connect is retried. The lock
    // serialises recreation so a fault can't spawn duplicate connects, and an in-flight (incomplete)
    // task is shared rather than restarted.
    private Task<IConnectionMultiplexer> GetConnectionTask()
    {
        lock (_connectionLock)
        {
            if (_redisConnectionTask is null || _redisConnectionTask.IsFaulted || _redisConnectionTask.IsCanceled)
            {
                _redisConnectionTask = Task.Run(async () =>
                {
                    using var scope = ProcessTimerFactory.Create("RedisCacheService_Connect");
                    var options = await GetConfigurationOptionsAsync() ?? throw new InvalidOperationException("Redis configuration options are not set");
                    return await _connectionFactory.ConnectAsync(options);
                });
            }

            return _redisConnectionTask;
        }
    }

    protected void StartConnection()
    {
        _ = GetConnectionTask();
    }

    internal async Task<IDatabase> RedisSetup()
    {
        var multiplexer = await GetConnectionTask();
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
        // Escape glob metacharacters in the LITERAL prefix before appending the wildcard. Redis KEYS
        // treats * ? [ ] \ as glob syntax, so a prefix derived from data (tenant id, email, ...) that
        // contains one would otherwise match the wrong keys - under-invalidating (an unterminated "["
        // matches nothing, leaving stale data) or over-invalidating (a "*" matches unrelated keys).
        // CreateWildcardActions is left unescaped by design: its caller is passing an actual pattern.
        return new RedisWildcardActions(this, EscapeGlobLiteral(prefix) + "*");
    }

    protected ICacheInvalidateActions CreateWildcardActions(string pattern)
    {
        return new RedisWildcardActions(this, pattern);
    }

    private static string EscapeGlobLiteral(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is '\\' or '*' or '?' or '[' or ']')
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }
}

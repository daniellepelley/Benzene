using StackExchange.Redis;

namespace Benzene.Cache.Redis;

public class RedisConnectionFactory : IRedisConnectionFactory
{
    public async Task<IConnectionMultiplexer> ConnectAsync(ConfigurationOptions options)
    {
        return await ConnectionMultiplexer.ConnectAsync(options);
    }
}

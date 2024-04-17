using StackExchange.Redis;

namespace Benzene.Cache.Redis;

public interface IRedisConnectionFactory
{
    Task<IConnectionMultiplexer> ConnectAsync(ConfigurationOptions options);
}

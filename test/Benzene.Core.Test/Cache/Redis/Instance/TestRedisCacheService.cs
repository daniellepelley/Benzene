using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Cache.Core;
using Benzene.Cache.Redis;
using Benzene.Diagnostics.Timers;
using StackExchange.Redis;

namespace Benzene.Test.Cache.Redis.Instance;

internal class TestRedisCacheService : RedisCacheService
{
    public TestRedisCacheService(IBenzeneLogger logger, IProcessTimerFactory processTimerFactory, IRedisConnectionFactory connectionFactory) : base(logger, processTimerFactory, connectionFactory)
    {
        StartConnection();
    }

    protected override Task<ConfigurationOptions> GetConfigurationOptionsAsync()
    {
        return Task.FromResult(new ConfigurationOptions());
    }

    public ICacheEntry<TestDataType> GetTestCacheEntry(int id)
    {
        return CreateCacheEntry<TestDataType>($"TEST_{id}");
    }

    public ICacheWriteActions<TestDataType> GetTestMultipleEntries(params int[] ids)
    {
        return CreateMultiKeyActions<TestDataType>(ids.Select(x => $"TEST_{x}"));
    }

    public ICacheInvalidateActions GetTestPrefixActions()
    {
        return CreatePrefixActions("TEST_");
    }

    public ICacheInvalidateActions GetTestWildcardActions()
    {
        return CreateWildcardActions("TEST_*");
    }
}

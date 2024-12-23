using System;
using System.Text.Json;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Cache.Core;
using Benzene.Cache.Redis;
using Benzene.Diagnostics.Timers;
using Benzene.Results;
using Benzene.Test.Cache.Redis.Instance;
using Benzene.Test.Cache.Redis.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Benzene.Test.Cache.Redis;

public class RedisCacheServiceTest
{
    const string TEST_ERROR_MESSAGE = "Test Error Message";


    [Fact]
    public async Task HealthCheckTest()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IBenzeneLogger>());
        services.AddScoped<IProcessTimerFactory>(_ => new DebugTimerFactory());
        services.AddScoped<IRedisConnectionFactory>(_ => new MockConnectionFactory());
        services.AddScoped<TestRedisCacheService>();

        var serviceResolver = new Microsoft.Dependencies.MicrosoftServiceResolverAdapter(services.BuildServiceProvider());

        var factory = new CacheHealthCheckFactory<TestRedisCacheService>();
        var healthcheck = factory.Create(serviceResolver);

        var result = await healthcheck.ExecuteAsync();

        Assert.Equal("ok", result.Status);
        Assert.Equal("Cache", result.Type);
        Assert.Equal(true, result.Data["CanConnect"]);
    }

    [Fact]
    public async Task FailedHealthCheckTest()
    {
        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.PingAsync(StackExchange.Redis.CommandFlags.None)).ThrowsAsync(new System.Exception(TEST_ERROR_MESSAGE));

        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IBenzeneLogger>());
        services.AddScoped<IProcessTimerFactory>(_ => new DebugTimerFactory());
        services.AddScoped<IRedisConnectionFactory>(_ => connectionFactory);
        services.AddScoped<TestRedisCacheService>();

        var serviceResolver = new Microsoft.Dependencies.MicrosoftServiceResolverAdapter(services.BuildServiceProvider());

        var factory = new CacheHealthCheckFactory<TestRedisCacheService>();
        var healthcheck = factory.Create(serviceResolver);

        var result = await healthcheck.ExecuteAsync();

        Assert.Equal("failed", result.Status);
        Assert.Equal("Cache", result.Type);
        Assert.Equal(false, result.Data["CanConnect"]);
        Assert.Equal(TEST_ERROR_MESSAGE, result.Data["Error"]);
    }

    [Fact]
    public async Task CacheEntryLazyLoadCacheHitTest()
    {
        var testValue = new TestDataType { Id = 42, Name = "Test" };
        var cacheValue = JsonSerializer.Serialize(testValue);

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(new RedisValue(cacheValue));

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.LazyLoadAsync(() => Task.FromResult(BenzeneResult.ServiceUnavailable<TestDataType>()));

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.Equivalent(testValue, result.Payload);
    }

    [Fact]
    public async Task CacheEntryLazyLoadCacheMissTest()
    {
        var testValue = new TestDataType { Id = 42, Name = "Test" };

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(RedisValue.Null);
        connectionFactory.DataBaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), When.Always, CommandFlags.None)).ReturnsAsync(true).Verifiable();

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.LazyLoadAsync(() => Task.FromResult(BenzeneResult.Ok(testValue)));

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
        Assert.Equivalent(testValue, result.Payload);
        connectionFactory.DataBaseMock.Verify();
    }

    [Fact]
    public async Task CacheEntryWriteThroughSimpleSetTest()
    {
        var testValue = new TestDataType { Id = 42, Name = "Test" };

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), When.Always, CommandFlags.None)).ReturnsAsync(true).Verifiable();

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Created(testValue)));

        Assert.Equal(BenzeneResultStatus.Created, result.Status);
        Assert.Equivalent(testValue, result.Payload);
        connectionFactory.DataBaseMock.Verify();
    }

    [Fact]
    public async Task CacheEntryWriteThroughSimpleDeleteTest()
    {
        var testValue = new TestDataType { Id = 42, Name = "Test" };

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true).Verifiable();

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Deleted(testValue)));

        Assert.Equal(BenzeneResultStatus.Deleted, result.Status);
        Assert.Equivalent(testValue, result.Payload);
        connectionFactory.DataBaseMock.Verify();
    }

    [Fact]
    public async Task CacheEntryWriteThroughSimpleNoWriteTest()
    {
        var connectionFactory = new MockConnectionFactory();
        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.BadRequest<TestDataType>(TEST_ERROR_MESSAGE)));

        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
        Assert.Equivalent(TEST_ERROR_MESSAGE, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task CacheEntryWriteThroughConvertSetTest()
    {
        var testValue = new TestDataType { Id = 42, Name = "Test" };

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), When.Always, CommandFlags.None)).ReturnsAsync(true).Verifiable();

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Updated(new { TestValue = testValue })), x => x.Payload.TestValue);

        Assert.Equal(BenzeneResultStatus.Updated, result.Status);
        Assert.Equivalent(testValue, result.Payload.TestValue);
        connectionFactory.DataBaseMock.Verify();
    }

    [Fact]
    public async Task CacheEntryWriteThroughConvertDeleteTest()
    {
        var testValue = new TestDataType { Id = 42, Name = "Test" };

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true).Verifiable();

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Deleted(new { TestValue = testValue })), x => x.Payload.TestValue);

        Assert.Equal(BenzeneResultStatus.Deleted, result.Status);
        Assert.Equivalent(testValue, result.Payload.TestValue);
        connectionFactory.DataBaseMock.Verify();
    }

    [Fact]
    public async Task CacheEntryWriteThroughConvertNoWriteTest()
    {
        var connectionFactory = new MockConnectionFactory();
        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.BadRequest<TestDataType>(TEST_ERROR_MESSAGE)), x => x.Payload);

        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
        Assert.Equivalent(TEST_ERROR_MESSAGE, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task CacheEntryWriteThroughInvlidateTest()
    {
        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true).Verifiable();

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughInvalidateAsync(() => Task.FromResult(BenzeneResult.Created("Test")));

        Assert.Equal(BenzeneResultStatus.Created, result.Status);
        Assert.Equivalent("Test", result.Payload);
        connectionFactory.DataBaseMock.Verify();
    }

    [Fact]
    public async Task CacheEntryWriteThroughInvlidateNoActionTest()
    {
        var connectionFactory = new MockConnectionFactory();
        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var entry = service.GetTestCacheEntry(42);

        var result = await entry.WriteThroughInvalidateAsync(() => Task.FromResult(BenzeneResult.BadRequest<TestDataType>(TEST_ERROR_MESSAGE)));

        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
        Assert.Equivalent(TEST_ERROR_MESSAGE, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task CacheMultipleEntriesTest()
    {
        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
        connectionFactory.DataBaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), When.Always, CommandFlags.None)).ReturnsAsync(true);

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var actions = service.GetTestMultipleEntries(23, 45);

        Assert.True(await actions.SetValueAsync(new TestDataType { Id = 42, Name = "Test" }));

        Assert.True(await actions.InvalidateAsync());
    }

    [Fact]
    public async Task CachePrefixTest()
    {
        var keys = RedisResult.Create(new[]
        {
            RedisResult.Create(new RedisKey("TEST_1")),
            RedisResult.Create(new RedisKey("TEST_2")),
        });

        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.ExecuteAsync("KEYS", It.IsAny<string>())).ReturnsAsync(keys);
        connectionFactory.DataBaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), CommandFlags.None)).ReturnsAsync(2);

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var actions = service.GetTestPrefixActions();

        var result = await actions.WriteThroughInvalidateAsync(() => Task.FromResult(BenzeneResult.Deleted()));

        Assert.Equal(BenzeneResultStatus.Deleted, result.Status);
    }

    [Fact]
    public async Task CacheWildcardTest()
    {
        var connectionFactory = new MockConnectionFactory();
        connectionFactory.DataBaseMock.Setup(x => x.ExecuteAsync("KEYS", It.IsAny<string>())).ThrowsAsync(new System.Exception(TEST_ERROR_MESSAGE));

        var service = new TestRedisCacheService(Mock.Of<IBenzeneLogger>(), new DebugTimerFactory(), connectionFactory);
        var actions = service.GetTestWildcardActions();

        Assert.False(await actions.InvalidateAsync());
    }
}

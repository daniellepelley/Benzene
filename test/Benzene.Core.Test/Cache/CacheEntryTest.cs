using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Cache.Core;
using Benzene.Diagnostics.Timers;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Test.Cache;

public class CacheEntryTest
{
    private class FakeCacheEntry<T> : CacheEntry<T>
    {
        private readonly Dictionary<string, string> _store;
        private readonly string _key;

        public bool ThrowOnGet;

        public FakeCacheEntry(Dictionary<string, string> store, string key = "the-key")
        {
            _store = store;
            _key = key;
        }

        protected override ILogger Logger => NullLogger.Instance;

        protected override IProcessTimerFactory ProcessTimerFactory => new DebugTimerFactory();

        protected override string KeyDescription => _key;

        protected override Task<string?> GetEntryValueAsync()
        {
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("cache read failed");
            }
            return Task.FromResult(_store.TryGetValue(_key, out var value) ? value : null);
        }

        protected override Task<bool> SetEntryValueAsync(string value, TimeSpan? expireIn)
        {
            _store[_key] = value;
            return Task.FromResult(true);
        }

        protected override Task<bool> InvalidateEntryAsync()
        {
            return Task.FromResult(_store.Remove(_key));
        }
    }

    [Fact]
    public async Task GetValueAsync_KeyPresent_DeserializesTheStoredValue()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("hello");

        var result = await entry.GetValueAsync();

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task GetValueAsync_KeyMissing_ReturnsDefault()
    {
        var entry = new FakeCacheEntry<string>(new Dictionary<string, string>());

        var result = await entry.GetValueAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetValueAsync_UnderlyingReadThrows_LogsAndReturnsDefaultInsteadOfThrowing()
    {
        var entry = new FakeCacheEntry<string>(new Dictionary<string, string>()) { ThrowOnGet = true };

        var result = await entry.GetValueAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SetValueAsync_SerializesAndStoresTheValue()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store, "my-key");

        var ok = await entry.SetValueAsync("hello");

        Assert.True(ok);
        Assert.True(store.ContainsKey("my-key"));
    }

    [Fact]
    public async Task InvalidateAsync_RemovesTheKey()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store, "my-key");
        await entry.SetValueAsync("hello");

        var removed = await entry.InvalidateAsync();

        Assert.True(removed);
        Assert.False(store.ContainsKey("my-key"));
    }

    [Fact]
    public async Task LazyLoadAsync_CacheHit_ReturnsTheCachedValueWithoutCallingDatabaseFunc()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("cached");
        var databaseFuncCalled = false;

        var result = await entry.LazyLoadAsync(() =>
        {
            databaseFuncCalled = true;
            return Task.FromResult(BenzeneResult.Ok("from-database"));
        });

        Assert.False(databaseFuncCalled);
        Assert.Equal("cached", result.Payload);
    }

    [Fact]
    public async Task LazyLoadAsync_CacheMiss_CallsDatabaseFuncAndWritesTheResultBackOnSuccess()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);

        var result = await entry.LazyLoadAsync(() => Task.FromResult(BenzeneResult.Ok("from-database")));

        Assert.Equal("from-database", result.Payload);
        Assert.True(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task LazyLoadAsync_CacheMiss_DatabaseFuncFails_DoesNotWriteToTheCache()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);

        var result = await entry.LazyLoadAsync(() => Task.FromResult(BenzeneResult.NotFound<string>()));

        Assert.False(result.IsSuccessful);
        Assert.False(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task LazyLoadAsync_ValueType_CacheMiss_CallsDatabaseFuncAndReturnsTheDbValue()
    {
        // Regression: for an unconstrained value-type T, a cold cache returns default(T) (e.g. 0),
        // and `default(int) != null` (boxed) is always true - so LazyLoad used to treat the MISS as a
        // hit, return 0, and never read the database. The hit decision must be presence-based.
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<int>(store);
        var databaseFuncCalled = false;

        var result = await entry.LazyLoadAsync(() =>
        {
            databaseFuncCalled = true;
            return Task.FromResult(BenzeneResult.Ok(42));
        });

        Assert.True(databaseFuncCalled);
        Assert.Equal(42, result.Payload);
        Assert.True(store.ContainsKey("the-key")); // the DB value was written back
    }

    [Fact]
    public async Task LazyLoadAsync_ValueType_CacheHitOfDefaultValue_IsAHitWithoutCallingDb()
    {
        // A genuinely-cached default value (0) is a hit, not a miss - presence, not `!= null`, decides.
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<int>(store);
        await entry.SetValueAsync(0);
        var databaseFuncCalled = false;

        var result = await entry.LazyLoadAsync(() =>
        {
            databaseFuncCalled = true;
            return Task.FromResult(BenzeneResult.Ok(99));
        });

        Assert.False(databaseFuncCalled);
        Assert.Equal(0, result.Payload);
    }

    [Fact]
    public async Task WriteThroughAsync_DefaultMapping_OkResult_SetsTheCache()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);

        await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Ok("new-value")));

        Assert.True(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task WriteThroughAsync_DefaultMapping_DeletedResult_InvalidatesTheCache()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("stale");

        await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Deleted<string>()));

        Assert.False(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task WriteThroughAsync_DefaultMapping_NotFoundResult_LeavesTheCacheUnchanged()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("unchanged");

        await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.NotFound<string>()));

        Assert.Equal("\"unchanged\"", store["the-key"]);
    }

    [Fact]
    public async Task WriteThroughAsync_CustomCacheValueMapping_UsesTheProvidedValue()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);

        await entry.WriteThroughAsync(
            () => Task.FromResult(BenzeneResult.Ok(42)),
            result => $"computed-{result.Payload}");

        Assert.True(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task WriteThroughAsync_CustomCacheActionMapping_UsesTheProvidedAction()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("stale");

        await entry.WriteThroughAsync(
            () => Task.FromResult(BenzeneResult.Ok(42)),
            result => $"computed-{result.Payload}",
            _ => CacheUpdateAction.Invalidate);

        Assert.False(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task WriteThroughInvalidateAsync_SuccessfulResult_InvalidatesTheCache()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("stale");

        await entry.WriteThroughInvalidateAsync(() => Task.FromResult(BenzeneResult.Ok()));

        Assert.False(store.ContainsKey("the-key"));
    }

    [Fact]
    public async Task WriteThroughInvalidateAsync_UnsuccessfulResult_LeavesTheCacheUnchanged()
    {
        var store = new Dictionary<string, string>();
        var entry = new FakeCacheEntry<string>(store);
        await entry.SetValueAsync("unchanged");

        await entry.WriteThroughInvalidateAsync(() => Task.FromResult(BenzeneResult.NotFound()));

        Assert.True(store.ContainsKey("the-key"));
    }
}

using Microsoft.Extensions.Logging;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public abstract class CacheEntry<T> : CacheWriteActions<T>, ICacheEntry<T>
{
    protected CacheEntry() : base()
    {
    }

    protected abstract Task<string?> GetEntryValueAsync();

    public async Task<T?> GetValueAsync()
    {
        var (_, value) = await TryReadEntryAsync();
        return value;
    }

    /// <summary>
    /// Reads the entry, returning whether the key was <em>present</em> (a real cache hit) separately
    /// from the deserialized value. The presence flag is what <see cref="GetEntryValueAsync"/> already
    /// knows (a non-empty stored string), and it's the only reliable hit signal for an unconstrained
    /// generic <typeparamref name="T"/>: for a value type, a genuine miss returns <c>default(T)</c>,
    /// and <c>default(T) != null</c> (via boxing) is always <c>true</c> - so deciding hit/miss from
    /// <c>value != null</c> mistakes every value-type miss for a hit of the default value.
    /// </summary>
    private async Task<(bool Found, T? Value)> TryReadEntryAsync()
    {
        try
        {
            Logger.LogDebug("Trying to hit cache key {key}", KeyDescription);
            var cacheValue = await GetEntryValueAsync();
            if (!string.IsNullOrEmpty(cacheValue))
            {
                return (true, Serializer.Deserialize<T>(cacheValue));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred when trying to read from cache");
        }
        return (false, default);
    }

    public Task<IBenzeneResult<T>> LazyLoadAsync(Func<Task<IBenzeneResult<T>>> databaseReadFunc)
    {
        return LazyLoadAsync(databaseReadFunc, value => BenzeneResult.Ok(value));
    }

    public async Task<TResult> LazyLoadAsync<TResult>(Func<Task<TResult>> databaseReadFunc, Func<T, TResult> createResult) where TResult : IBenzeneResult<T>
    {
        using var timerScope = ProcessTimerFactory.Create("CacheEntry_LazyLoad");

        var (found, cacheValue) = await TryReadEntryAsync();

        // Hit requires the key to have been present. The `is not null` keeps the existing behavior
        // for reference-type T (a present-but-null-deserialized value stays a miss, i.e. the current
        // negative-caching/penetration behavior is unchanged); for a value type `found` is what
        // corrects the miss-as-hit bug (`cacheValue is not null` is always true there).
        if (found && cacheValue is not null)
        {
            timerScope.SetTag("cache-status", "hit");
            Logger.LogDebug("Cache hit for key {key}", KeyDescription);
            return createResult(cacheValue);
        }
        else
        {
            timerScope.SetTag("cache-status", "miss");
            Logger.LogDebug("No hit in cache for key {key}", KeyDescription);

            var benzeneResult = await databaseReadFunc();

            if (benzeneResult.IsSuccessful)
            {
                await SetValueAsync(benzeneResult.Payload);
            }

            return benzeneResult;
        }
    }
}

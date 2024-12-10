using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Serialization;
using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public abstract class CacheWriteActions<T> : CacheInvalidateActions, ICacheWriteActions<T>
{
    protected ISerializer Serializer { get; }

    protected CacheWriteActions()
    {
        Serializer = new JsonSerializer();
    }

    protected abstract Task<bool> SetEntryValueAsync(string value, TimeSpan? expireIn);

    public async Task<bool> SetValueAsync(T value, TimeSpan? expireIn = null)
    {
        Logger.LogDebug("Setting cache for key {key}", KeyDescription);
        var cacheValue = Serializer.Serialize(value);
        return await SetEntryValueAsync(cacheValue, expireIn);
    }

    private static CacheUpdateAction DefaultCacheActionMapping<TResult>(TResult result) where TResult : IResult
    {
        return result.Status switch
        {
            ServiceResultStatus.Accepted or
            ServiceResultStatus.Ok or
            ServiceResultStatus.Created or
            ServiceResultStatus.Updated => CacheUpdateAction.Set,
            ServiceResultStatus.Deleted => CacheUpdateAction.Invalidate,
            _ => CacheUpdateAction.None,
        };
    }

    public Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc) where TResult : IResult<T>
    {
        return WriteThroughAsync(modifyDatabaseFunc, result => result.Payload, DefaultCacheActionMapping);
    }

    public Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc, Func<TResult, T> getCacheValue) where TResult : IResult
    {
        return WriteThroughAsync(modifyDatabaseFunc, getCacheValue, DefaultCacheActionMapping);
    }

    public async Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc, Func<TResult, T> getCacheValue, Func<TResult, CacheUpdateAction> getCacheAction) where TResult : IResult
    {
        using var timerScope = ProcessTimerFactory.Create("CacheActions_WriteThrough");

        var result = await modifyDatabaseFunc();

        switch (getCacheAction(result))
        {
            case CacheUpdateAction.Set:
                timerScope.SetTag("cache-action", "set");
                await SetValueAsync(getCacheValue(result));
                break;

            case CacheUpdateAction.Invalidate:
                timerScope.SetTag("cache-action", "invalidate");
                await InvalidateAsync();
                break;

            default:
                timerScope.SetTag("cache-action", "none");
                Logger.LogDebug("Cache unchanged for key {key}", KeyDescription);
                break;
        }

        return result;
    }
}

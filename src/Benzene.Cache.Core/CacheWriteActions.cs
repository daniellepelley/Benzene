using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;
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

    private static CacheUpdateAction DefaultCacheActionMapping<TResult>(TResult result) where TResult : IBenzeneResult
    {
        return result.Status switch
        {
            BenzeneResultStatus.Accepted or
            BenzeneResultStatus.Ok or
            BenzeneResultStatus.Created or
            BenzeneResultStatus.Updated => CacheUpdateAction.Set,
            BenzeneResultStatus.Deleted => CacheUpdateAction.Invalidate,
            _ => CacheUpdateAction.None,
        };
    }

    public Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc) where TResult : IBenzeneResult<T>
    {
        return WriteThroughAsync(modifyDatabaseFunc, result => result.Payload, DefaultCacheActionMapping);
    }

    public Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc, Func<TResult, T> getCacheValue) where TResult : IBenzeneResult
    {
        return WriteThroughAsync(modifyDatabaseFunc, getCacheValue, DefaultCacheActionMapping);
    }

    public async Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc, Func<TResult, T> getCacheValue, Func<TResult, CacheUpdateAction> getCacheAction) where TResult : IBenzeneResult
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

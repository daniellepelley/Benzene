using Benzene.Abstractions.Logging;
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
        try
        {
            Logger.LogDebug("Trying to hit cache key {key}", KeyDescription);
            var cacheValue = await GetEntryValueAsync();
            if (!string.IsNullOrEmpty(cacheValue))
            {
                return Serializer.Deserialize<T>(cacheValue);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred when trying to read from cache");
        }
        return default;
    }

    public Task<IClientResult<T>> LazyLoadAsync(Func<Task<IClientResult<T>>> databaseReadFunc)
    {
        return LazyLoadAsync(databaseReadFunc, value => ClientResult.Ok(value));
    }

    public Task<IServiceResult<T>> LazyLoadAsync(Func<Task<IServiceResult<T>>> databaseReadFunc)
    {
        return LazyLoadAsync(databaseReadFunc, value => ServiceResult.Ok(value));
    }

    public async Task<TResult> LazyLoadAsync<TResult>(Func<Task<TResult>> databaseReadFunc, Func<T, TResult> createResult) where TResult : IResult<T>
    {
        using var timerScope = ProcessTimerFactory.Create("CacheEntry_LazyLoad");

        var cacheValue = await GetValueAsync();

        if (cacheValue != null)
        {
            timerScope.SetTag("cache-status", "hit");
            Logger.LogDebug("Cache hit for key {key}", KeyDescription);
            return createResult(cacheValue);
        }
        else
        {
            timerScope.SetTag("cache-status", "miss");
            Logger.LogDebug("No hit in cache for key {key}", KeyDescription);

            var serviceResult = await databaseReadFunc();

            if (serviceResult.IsSuccessful)
            {
                await SetValueAsync(serviceResult.Payload);
            }

            return serviceResult;
        }
    }
}

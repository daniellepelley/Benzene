using Benzene.Abstractions.Logging;
using Benzene.Diagnostics.Timers;
using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public abstract class CacheInvalidateActions : ICacheInvalidateActions
{
    protected abstract IBenzeneLogger Logger { get; }
    protected abstract IProcessTimerFactory ProcessTimerFactory { get; }
    protected abstract string KeyDescription { get; }

    protected abstract Task<bool> InvalidateEntryAsync();

    public Task<bool> InvalidateAsync()
    {
        Logger.LogDebug("Invalidating cache for key {key}", KeyDescription);
        return InvalidateEntryAsync();
    }

    public async Task<TResult> WriteThroughInvalidateAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc) where TResult : IResult
    {
        using var timerScope = ProcessTimerFactory.Create("CacheActions_WriteThrough");

        var result = await modifyDatabaseFunc();

        if (result.IsSuccessful)
        {
            timerScope.SetTag("cache-action", "invalidate");
            await InvalidateAsync();
        }
        else
        {
            timerScope.SetTag("cache-action", "none");
            Logger.LogDebug("Cache unchanged for key {key}", KeyDescription);
        }

        return result;
    }
}

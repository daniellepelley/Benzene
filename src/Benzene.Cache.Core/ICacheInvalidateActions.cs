using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public interface ICacheInvalidateActions
{
    Task<bool> InvalidateAsync();

    Task<TResult> WriteThroughInvalidateAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc) where TResult : IBenzeneResult;
}

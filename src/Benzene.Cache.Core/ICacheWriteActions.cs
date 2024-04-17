﻿using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public interface ICacheWriteActions<T> : ICacheInvalidateActions
{
    Task<bool> SetValueAsync(T value, TimeSpan? expireIn = null);

    Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc) where TResult : IResult<T>;

    Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc, Func<TResult, T> getCacheValue) where TResult : IResult;

    Task<TResult> WriteThroughAsync<TResult>(Func<Task<TResult>> modifyDatabaseFunc, Func<TResult, T> getCacheValue, Func<TResult, CacheUpdateAction> getCacheAction) where TResult : IResult;
}

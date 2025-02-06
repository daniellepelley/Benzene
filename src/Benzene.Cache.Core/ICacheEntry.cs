using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public interface ICacheEntry<T> : ICacheWriteActions<T>
{
    Task<T?> GetValueAsync();

    Task<IBenzeneResult<T>> LazyLoadAsync(Func<Task<IBenzeneResult<T>>> databaseReadFunc);

    Task<TResult> LazyLoadAsync<TResult>(Func<Task<TResult>> databaseReadFunc, Func<T, TResult> createResult) where TResult : IBenzeneResult<T>;
}

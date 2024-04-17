using Benzene.Results;

namespace Benzene.Cache.Core;

#nullable enable

public interface ICacheEntry<T> : ICacheWriteActions<T>
{
    Task<T?> GetValueAsync();

    Task<IClientResult<T>> LazyLoadAsync(Func<Task<IClientResult<T>>> databaseReadFunc);

    Task<IServiceResult<T>> LazyLoadAsync(Func<Task<IServiceResult<T>>> databaseReadFunc);

    Task<TResult> LazyLoadAsync<TResult>(Func<Task<TResult>> databaseReadFunc, Func<T, TResult> createResult) where TResult : IResult<T>;
}

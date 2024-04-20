using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Results;

public static class ClientResultExtensions
{
    public static IHandlerResult<T> AsHandlerResult<T>(this IClientResult<T> clientResult)
    {
        return AsHandlerResult(clientResult, clientResult.Payload);
    }

    public static IHandlerResult<TOutput> AsHandlerResult<TInput, TOutput>(this IClientResult<TInput> clientResult, TOutput payload)
    {
        switch (clientResult.Status)
        {
            case ClientResultStatus.Accepted:
                return HandlerResult.Accepted(payload);
            case ClientResultStatus.Success:
                return HandlerResult.Ok(payload);
            case ClientResultStatus.Created:
                return HandlerResult.Created(payload);
            case ClientResultStatus.Updated:
                return HandlerResult.Updated(payload);
            case ClientResultStatus.Deleted:
                return HandlerResult.Deleted(payload);
            case ClientResultStatus.NotFound:
                return HandlerResult.NotFound<TOutput>(clientResult.Errors);
            case ClientResultStatus.Conflict:
                return HandlerResult.Conflict<TOutput>(clientResult.Errors);
            case ClientResultStatus.ServiceUnavailable:
            case ClientResultStatus.UnexpectedError:
                return HandlerResult.ServiceUnavailable<TOutput>(clientResult.Errors);
            default:
                return HandlerResult.ServiceUnavailable<TOutput>();
        }
    }

    public static IHandlerResult<T> AsHandlerResult<T>(this IClientResult clientResult)
    {
        switch (clientResult.Status)
        {
            case ClientResultStatus.Accepted:
                return HandlerResult.Accepted<T>();
            case ClientResultStatus.Success:
                return HandlerResult.Ok<T>();
            case ClientResultStatus.Created:
                return HandlerResult.Created<T>();
            case ClientResultStatus.Updated:
                return HandlerResult.Updated<T>();
            case ClientResultStatus.Deleted:
                return HandlerResult.Deleted<T>();
            case ClientResultStatus.NotFound:
                return HandlerResult.NotFound<T>(clientResult.Errors);
            case ClientResultStatus.Conflict:
                return HandlerResult.Conflict<T>(clientResult.Errors);
            case ClientResultStatus.ServiceUnavailable:
            case ClientResultStatus.UnexpectedError:
                return HandlerResult.ServiceUnavailable<T>(clientResult.Errors);
            default:
                return HandlerResult.ServiceUnavailable<T>();
        }
    }

    public static IHandlerResult AsHandlerResult(this IClientResult clientResult)
    {
        switch (clientResult.Status)
        {
            case ClientResultStatus.Accepted:
                return HandlerResult.Accepted(clientResult.PayloadAsObject);
            case ClientResultStatus.Success:
                return HandlerResult.Ok(clientResult.PayloadAsObject);
            case ClientResultStatus.Created:
                return HandlerResult.Created(clientResult.PayloadAsObject);
            case ClientResultStatus.Updated:
                return HandlerResult.Updated(clientResult.PayloadAsObject);
            case ClientResultStatus.Deleted:
                return HandlerResult.Deleted();
            case ClientResultStatus.NotFound:
                return HandlerResult.NotFound(clientResult.Errors);
            case ClientResultStatus.Conflict:
                return HandlerResult.Conflict(clientResult.Errors);
            case ClientResultStatus.ServiceUnavailable:
            case ClientResultStatus.UnexpectedError:
                return HandlerResult.ServiceUnavailable(clientResult.Errors);
            default:
                return HandlerResult.ServiceUnavailable();
        }
    }

    public static Task<IHandlerResult<T>> AsHandlerResult<T>(this Task<IClientResult<T>> source)
    {
        return source.ContinueWith(clientResult => clientResult.Result.AsHandlerResult());
    }

    public static async Task<IHandlerResult<TOutput>> AsHandlerResultMapIfSuccessful<TInput, TOutput>(this Task<IClientResult<TInput>> source, Func<TInput, TOutput> map)
    {
        await source;
        if (source.Result.IsSuccessful)
        {
            return source.Result.AsHandlerResult(map(source.Result.Payload));

        }

        return source.Result.AsHandlerResult<TOutput>();
    }

    // public static bool IsSuccessful<T>(this IClientResult<T> source)
    // {
    //     return new[]
    //     {
    //         ClientResultStatus.Success,
    //         ClientResultStatus.Created,
    //         ClientResultStatus.Updated,
    //         ClientResultStatus.Deleted
    //     }.Contains(source.Status);
    // }

    public static async Task<IClientResult<TOutput>> MapIfSuccessful<TInput, TOutput>(this Task<IClientResult<TInput>> source, Func<TInput, TOutput> map)
    {
        await source;
        if (source.Result.IsSuccessful)
        {
            return source.Result.AsType(map(source.Result.Payload));

        }

        return source.Result.AsType<TOutput>();
    }

    public static IClientResult<TOutput> AsType<TInput, TOutput>(this IClientResult<TInput> clientResult, TOutput payload)
    {
        switch (clientResult.Status)
        {
            case ClientResultStatus.Accepted:
                return ClientResult.Accepted(payload);
            case ClientResultStatus.Success:
                return ClientResult.Ok(payload);
            case ClientResultStatus.Created:
                return ClientResult.Created(payload);
            case ClientResultStatus.Updated:
                return ClientResult.Updated(payload);
            case ClientResultStatus.Deleted:
                return ClientResult.Deleted(payload);
            case ClientResultStatus.NotFound:
                return ClientResult.NotFound<TOutput>(clientResult.Errors);
            case ClientResultStatus.Conflict:
                return ClientResult.Conflict<TOutput>(clientResult.Errors);
            case ClientResultStatus.ServiceUnavailable:
            case ClientResultStatus.UnexpectedError:
                return ClientResult.ServiceUnavailable<TOutput>(clientResult.Errors);
            default:
                return ClientResult.ServiceUnavailable<TOutput>();
        }
    }

    public static IClientResult<T> AsType<T>(this IClientResult clientResult)
    {
        switch (clientResult.Status)
        {
            case ClientResultStatus.Accepted:
                return ClientResult.Accepted<T>();
            case ClientResultStatus.Success:
                return ClientResult.Ok<T>();
            case ClientResultStatus.Created:
                return ClientResult.Created<T>();
            case ClientResultStatus.Updated:
                return ClientResult.Updated<T>();
            case ClientResultStatus.Deleted:
                return ClientResult.Deleted<T>();
            case ClientResultStatus.NotFound:
                return ClientResult.NotFound<T>(clientResult.Errors);
            case ClientResultStatus.Conflict:
                return ClientResult.Conflict<T>(clientResult.Errors);
            case ClientResultStatus.ServiceUnavailable:
            case ClientResultStatus.UnexpectedError:
                return ClientResult.ServiceUnavailable<T>(clientResult.Errors);
            default:
                return ClientResult.ServiceUnavailable<T>();
        }
    }
}
using System.Net;
using Benzene.Abstractions.Results;

namespace Benzene.Results;

public static class BenzeneResultExtensions
{
    public static bool IsAccepted(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Accepted;
    }
    public static bool IsSuccess(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Ok;
    }
    public static bool IsOk(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Ok;
    }

    public static bool IsCreated(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Created;
    }
    public static bool IsUpdated(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Updated;
    }
    public static bool IsDeleted(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Deleted;
    }
    public static bool IsIgnored(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Ignored;
    }
    public static bool IsNotFound(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.NotFound;
    }
    public static bool IsBadRequest(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.BadRequest;
    }
    public static bool IsValidationError(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.ValidationError;
    }
    public static bool IsServiceUnavailable(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.ServiceUnavailable;
    }
    public static bool IsNotImplemented(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.NotImplemented;
    }
    public static bool IsUnexpectedError(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.UnexpectedError;
    }
    public static bool IsConflict(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Conflict;
    }
    public static bool IsForbidden(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Forbidden;
    }
    public static bool IsUnauthorized(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.Status == BenzeneResultStatus.Unauthorized;
    }

    public static IBenzeneResult<TOutput> As<T, TOutput>(this IBenzeneResult<T> serviceBenzeneResult)
    {
        return As<TOutput>(serviceBenzeneResult);
    }

    public static IBenzeneResult<TOutput> As<TOutput>(this IBenzeneResult serviceBenzeneResult)
    {
        return serviceBenzeneResult.IsSuccessful
           ? BenzeneResult.Set<TOutput>(serviceBenzeneResult.Status)
           : BenzeneResult.Set<TOutput>(serviceBenzeneResult.Status, serviceBenzeneResult.Errors);
    }

    public static IBenzeneResult<TOutput> As<T, TOutput>(this IBenzeneResult<T> serviceBenzeneResult,
        Func<T, TOutput> map)
    {
        return serviceBenzeneResult.IsSuccessful
           ? BenzeneResult.Set(serviceBenzeneResult.Status, map(serviceBenzeneResult.Payload))
           : BenzeneResult.Set<TOutput>(serviceBenzeneResult.Status, serviceBenzeneResult.Errors);
    }

    public static IBenzeneResult<TOutput> As<TOutput>(this IBenzeneResult serviceBenzeneResult, TOutput value)
    {
        return BenzeneResult.Set(serviceBenzeneResult.Status, value);
    }

    public static async Task<IBenzeneResult<TOutput>> As<T, TOutput>(this Task<IBenzeneResult<T>> source,
        Func<T, TOutput> map)
    {
        await source;
        return source.Result.As(map);
    }

    public static async Task<IBenzeneResult<TOutput>> As<TOutput>(this Task<IBenzeneResult> source,
        TOutput value)
    {
        await source;
        return source.Result.As(value);
    }

    public static async Task<IBenzeneResult<TOutput>> As<TOutput>(this Task<IBenzeneResult> source)
    {
        await source;
        return source.Result.As<TOutput>();
    }

    public static async Task<IBenzeneResult<TOutput>> As<T, TOutput>(this Task<IBenzeneResult<T>> source)
    {
        await source;
        return source.Result.As<T, TOutput>();
    }

    public static Task<IBenzeneResult<T>> AsTask<T>(this IBenzeneResult<T> serviceBenzeneResult)
    {
        return Task.FromResult(serviceBenzeneResult);
    }

    public static IBenzeneResult Convert(this HttpStatusCode httpStatusCode)
    {
        return httpStatusCode switch
        {
            HttpStatusCode.Accepted => BenzeneResult.Accepted(),
            HttpStatusCode.OK => BenzeneResult.Ok(),
            HttpStatusCode.Created => BenzeneResult.Created(),
            HttpStatusCode.NoContent => BenzeneResult.Deleted(),
            HttpStatusCode.NotFound => BenzeneResult.NotFound(),
            HttpStatusCode.BadRequest => BenzeneResult.BadRequest(),
            HttpStatusCode.Conflict => BenzeneResult.Conflict(),
            HttpStatusCode.Forbidden => BenzeneResult.Forbidden(),
            HttpStatusCode.Unauthorized => BenzeneResult.Unauthorized(),
            _ => BenzeneResult.UnexpectedError()
        };
    }
    
    public static IBenzeneResult<T> Convert<T>(this HttpStatusCode httpStatusCode, T payload)
    {
        return httpStatusCode switch
        {
            HttpStatusCode.Accepted => BenzeneResult.Accepted(payload),
            HttpStatusCode.OK => BenzeneResult.Ok(payload),
            HttpStatusCode.Created => BenzeneResult.Created(payload),
            HttpStatusCode.NoContent => BenzeneResult.Deleted(payload),
            HttpStatusCode.NotFound => BenzeneResult.NotFound<T>(),
            HttpStatusCode.BadRequest => BenzeneResult.BadRequest<T>(),
            HttpStatusCode.Conflict => BenzeneResult.Conflict<T>(),
            HttpStatusCode.Forbidden => BenzeneResult.Forbidden<T>(),
            HttpStatusCode.Unauthorized => BenzeneResult.Unauthorized<T>(),
            _ => BenzeneResult.UnexpectedError<T>()
        };
    }

    public static IBenzeneResult<T> Convert<T>(this HttpStatusCode httpStatusCode)
    {
        return httpStatusCode switch
        {
            HttpStatusCode.Accepted => BenzeneResult.Accepted<T>(),
            HttpStatusCode.OK => BenzeneResult.Ok<T>(),
            HttpStatusCode.Created => BenzeneResult.Created<T>(),
            HttpStatusCode.NoContent => BenzeneResult.Deleted<T>(),
            HttpStatusCode.NotFound => BenzeneResult.NotFound<T>(),
            HttpStatusCode.BadRequest => BenzeneResult.BadRequest<T>(),
            HttpStatusCode.Conflict => BenzeneResult.Conflict<T>(),
            HttpStatusCode.Forbidden => BenzeneResult.Forbidden<T>(),
            HttpStatusCode.Unauthorized => BenzeneResult.Unauthorized<T>(),
            _ => BenzeneResult.UnexpectedError<T>()
        };
    }
}

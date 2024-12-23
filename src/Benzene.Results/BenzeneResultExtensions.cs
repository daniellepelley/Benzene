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

    public static string AsHttpStatusCode(this string benzeneResultStatus)
    {
        switch (benzeneResultStatus)
        {
            case BenzeneResultStatus.Ok:
            case BenzeneResultStatus.Ignored:
                return "200";
            case BenzeneResultStatus.Created:
                return "201";
            case BenzeneResultStatus.Accepted:
                return "202";
            case BenzeneResultStatus.Updated:
                return "204";
            case BenzeneResultStatus.Deleted:
                return "204";
            case BenzeneResultStatus.BadRequest:
                return "400";
            case BenzeneResultStatus.Unauthorized:
                return "401";
            case BenzeneResultStatus.Forbidden:
                return "403";
            case BenzeneResultStatus.NotFound:
                return "404";
            case BenzeneResultStatus.Conflict:
                return "409";
            case BenzeneResultStatus.ValidationError:
                return "422";
            case BenzeneResultStatus.NotImplemented:
                return "501";
            case BenzeneResultStatus.ServiceUnavailable:
                return "503";
            default:
                return "500";
        }
    }

    public static Task<IBenzeneResult<T>> AsTask<T>(this IBenzeneResult<T> serviceBenzeneResult)
    {
        return Task.FromResult(serviceBenzeneResult);
    }
}

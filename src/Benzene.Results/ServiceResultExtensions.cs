namespace Benzene.Results;

public static class ServiceResultExtensions
{
    public static bool IsAccepted(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Accepted;
    }
    public static bool IsSuccess(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Ok;
    }
    public static bool IsOk(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Ok;
    }

    public static bool IsCreated(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Created;
    }
    public static bool IsUpdated(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Updated;
    }
    public static bool IsDeleted(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Deleted;
    }
    public static bool IsIgnored(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Ignored;
    }
    public static bool IsNotFound(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.NotFound;
    }
    public static bool IsBadRequest(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.BadRequest;
    }
    public static bool IsValidationError(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.ValidationError;
    }
    public static bool IsServiceUnavailable(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.ServiceUnavailable;
    }
    public static bool IsNotImplemented(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.NotImplemented;
    }
    public static bool IsUnexpectedError(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.UnexpectedError;
    }
    public static bool IsConflict(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Conflict;
    }
    public static bool IsForbidden(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Forbidden;
    }
    public static bool IsUnauthorized(this IServiceResult serviceResult)
    {
        return serviceResult.Status == ServiceResultStatus.Unauthorized;
    }

    public static IServiceResult<TOutput> As<T, TOutput>(this IServiceResult<T> serviceResult)
    {
        return As<TOutput>(serviceResult);
    }

    public static IServiceResult<TOutput> As<TOutput>(this IServiceResult serviceResult)
    {
        return serviceResult.IsSuccessful
           ? ServiceResult.Set<TOutput>(serviceResult.Status)
           : ServiceResult.Set<TOutput>(serviceResult.Status, serviceResult.Errors);
    }

    public static IServiceResult<TOutput> As<T, TOutput>(this IServiceResult<T> serviceResult,
        Func<T, TOutput> map)
    {
        return serviceResult.IsSuccessful
           ? ServiceResult.Set(serviceResult.Status, map(serviceResult.Payload))
           : ServiceResult.Set<TOutput>(serviceResult.Status, serviceResult.Errors);
    }

    public static IServiceResult<TOutput> As<TOutput>(this IServiceResult serviceResult, TOutput value)
    {
        return ServiceResult.Set(serviceResult.Status, value);
    }

    public static async Task<IServiceResult<TOutput>> As<T, TOutput>(this Task<IServiceResult<T>> source,
        Func<T, TOutput> map)
    {
        await source;
        return source.Result.As(map);
    }

    public static async Task<IServiceResult<TOutput>> As<TOutput>(this Task<IServiceResult> source,
        TOutput value)
    {
        await source;
        return source.Result.As(value);
    }

    public static async Task<IServiceResult<TOutput>> As<TOutput>(this Task<IServiceResult> source)
    {
        await source;
        return source.Result.As<TOutput>();
    }

    public static async Task<IServiceResult<TOutput>> As<T, TOutput>(this Task<IServiceResult<T>> source)
    {
        await source;
        return source.Result.As<T, TOutput>();
    }

    public static string AsHttpStatusCode(this string serviceResultStatus)
    {
        switch (serviceResultStatus)
        {
            case ServiceResultStatus.Ok:
            case ServiceResultStatus.Ignored:
                return "200";
            case ServiceResultStatus.Created:
                return "201";
            case ServiceResultStatus.Accepted:
                return "202";
            case ServiceResultStatus.Updated:
                return "204";
            case ServiceResultStatus.Deleted:
                return "204";
            case ServiceResultStatus.BadRequest:
                return "400";
            case ServiceResultStatus.Unauthorized:
                return "401";
            case ServiceResultStatus.Forbidden:
                return "403";
            case ServiceResultStatus.NotFound:
                return "404";
            case ServiceResultStatus.Conflict:
                return "409";
            case ServiceResultStatus.ValidationError:
                return "422";
            case ServiceResultStatus.NotImplemented:
                return "501";
            case ServiceResultStatus.ServiceUnavailable:
                return "503";
            default:
                return "500";
        }
    }

    public static Task<IServiceResult<T>> AsTask<T>(this IServiceResult<T> serviceResult)
    {
        return Task.FromResult(serviceResult);
    }
}

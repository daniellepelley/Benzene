namespace Benzene.Results
{
    public static class ClientResultExtensions
    {
        public static bool IsAccepted(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Accepted;
        }
        [Obsolete("Use IsOk instead", false)]
        public static bool IsSuccess(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Success;
        }
        public static bool IsOk(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Ok;
        }
        public static bool IsCreated(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Created;
        }
        public static bool IsUpdated(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Updated;
        }
        public static bool IsDeleted(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Deleted;
        }
        public static bool IsIgnored(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Ignored;
        }
        public static bool IsNotFound(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.NotFound;
        }
        public static bool IsBadRequest(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.BadRequest;
        }
        public static bool IsValidationError(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.ValidationError;
        }
        public static bool IsServiceUnavailable(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.ServiceUnavailable;
        }
        public static bool IsNotImplemented(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.NotImplemented;
        }
        public static bool IsUnexpectedError(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.UnexpectedError;
        }
        public static bool IsConflict(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Conflict;
        }
        public static bool IsForbidden(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Forbidden;
        }
        public static bool IsUnauthorized(this IClientResult clientResult)
        {
            return clientResult.Status == ClientResultStatus.Unauthorized;
        }

        public static IServiceResult<T> AsServiceResult<T>(this IClientResult<T> clientResult)
        {
            return AsServiceResult(clientResult, clientResult.Payload);
        }

        public static IServiceResult<TOutput> AsServiceResult<TInput, TOutput>(this IClientResult<TInput> clientResult, TOutput payload)
        {
            return clientResult.IsSuccessful
                ? ServiceResult.Set(clientResult.Status, payload)
                : ServiceResult.Set<TOutput>(clientResult.Status, clientResult.Errors);
        }

        public static IServiceResult<T> AsServiceResult<T>(this IClientResult clientResult)
        {
            return clientResult.IsSuccessful
                ? ServiceResult.Set<T>(clientResult.Status)
                : ServiceResult.Set<T>(clientResult.Status, clientResult.Errors);
        }
        
        public static IServiceResult AsServiceResult(this IClientResult clientResult)
        {
            return clientResult.IsSuccessful
                ? ServiceResult.Set(clientResult.Status, clientResult.PayloadAsObject)
                : ServiceResult.Set(clientResult.Status, clientResult.Errors);
        }

        public static Task<IServiceResult<T>> AsServiceResult<T>(this Task<IClientResult<T>> source)
        {
            return source.ContinueWith(clientResult => clientResult.Result.AsServiceResult());
        }
        
        public static Task<IServiceResult> AsServiceResult(this Task<IClientResult> source)
        {
            return source.ContinueWith(clientResult => clientResult.Result.AsServiceResult());
        }

        public static async Task<IServiceResult<TOutput>> AsServiceResultMapIfSuccessful<TInput, TOutput>(this Task<IClientResult<TInput>> source, Func<TInput, TOutput> map)
        {
            await source;
            return source.Result.IsSuccessful
                ? source.Result.AsServiceResult(map(source.Result.Payload))
                : source.Result.AsServiceResult<TOutput>();
        }

        public static async Task<IClientResult<TOutput>> MapIfSuccessful<TInput, TOutput>(this Task<IClientResult<TInput>> source, Func<TInput, TOutput> map)
        {
            await source;
            return source.Result.IsSuccessful
                ? source.Result.As(map(source.Result.Payload))
                : source.Result.As<TOutput>();
        }

        public static IClientResult<TOutput> As<TInput, TOutput>(this IClientResult<TInput> clientResult, TOutput payload)
        {
            return clientResult.IsSuccessful
                ? ClientResult.Set(clientResult.Status, payload)
                : ClientResult.Set<TOutput>(clientResult.Status, clientResult.Errors);
        }
        
        public static IClientResult<T> As<T>(this IClientResult clientResult)
        {
            return clientResult.IsSuccessful
                ? ClientResult.Set<T>(clientResult.Status, true)
                : ClientResult.Set<T>(clientResult.Status, clientResult.Errors);
        }
    }
}

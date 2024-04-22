namespace Benzene.Results;

public static class ServiceResult
{
    public static IServiceResult Set(string status)
    {
        return Set(status, new Void());
    }

    public static IServiceResult<T> Set<T>(string status, bool isSuccessful)
    {
        return ServiceResultInternal<T>.Internal(status, isSuccessful);
    }

    public static IServiceResult<T> Set<T>(string status, T payload)
    {
        return ServiceResultInternal<T>.Internal(status, payload);
    }

    public static IServiceResult<T> Set<T>(string status, params string[] errors)
    {
        return ServiceResultInternal<T>.Internal(status, errors);
    }

    public static IServiceResult Ok()
    {
        return Ok(new Void());
    }

    public static IServiceResult<T> Ok<T>()
    {
        return ServiceResultInternal<T>.OkInternal(default);
    }
    
    public static IServiceResult<T> Ok<T>(T payload)
    {
        return ServiceResultInternal<T>.OkInternal(payload);
    }

    public static IServiceResult Created()
    {
        return Created(new Void());
    }

    public static IServiceResult<T> Created<T>()
    {
        return ServiceResultInternal<T>.CreatedInternal(default);
    }

    public static IServiceResult<T> Created<T>(T payload)
    {
        return ServiceResultInternal<T>.CreatedInternal(payload);
    }

    public static IServiceResult Accepted()
    {
        return Accepted(new Void());
    }

    public static IServiceResult<T> Accepted<T>()
    {
        return ServiceResultInternal<T>.AcceptedInternal(default);
    }

    public static IServiceResult<T> Accepted<T>(T payload)
    {
        return ServiceResultInternal<T>.AcceptedInternal(payload);
    }

    public static IServiceResult Updated()
    {
        return Updated(new Void());
    }

    public static IServiceResult<T> Updated<T>()
    {
        return ServiceResultInternal<T>.UpdatedInternal(default);
    }

    public static IServiceResult<T> Updated<T>(T payload)
    {
        return ServiceResultInternal<T>.UpdatedInternal(payload);
    }

    public static IServiceResult Deleted()
    {
        return Deleted(new Void());
    }

    public static IServiceResult<T> Deleted<T>()
    {
        return ServiceResultInternal<T>.DeletedInternal(default);
    }

    public static IServiceResult<T> Deleted<T>(T payload)
    {
        return ServiceResultInternal<T>.DeletedInternal(payload);
    }

    public static IServiceResult Ignored()
    {
        return ServiceResultInternal<Void>.IgnoredInternal();
    }

    public static IServiceResult<T> Ignored<T>()
    {
        return ServiceResultInternal<T>.IgnoredInternal();
    }

    public static IServiceResult ValidationError(params string[] errors)
    {
        return ServiceResultInternal<Void>.ValidationErrorInternal(errors);
    }

    public static IServiceResult<T> ValidationError<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.ValidationErrorInternal(errors);
    }

    public static IServiceResult NotFound(params string[] errors)
    {
        return ServiceResultInternal<Void>.NotFoundInternal(errors);
    }

    public static IServiceResult<T> NotFound<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.NotFoundInternal(errors);
    }
    public static IServiceResult BadRequest(params string[] errors)
    {
        return ServiceResultInternal<Void>.BadRequestInternal(errors);
    }

    public static IServiceResult<T> BadRequest<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.BadRequestInternal(errors);
    }

    public static IServiceResult Forbidden(params string[] errors)
    {
        return ServiceResultInternal<Void>.ForbiddenInternal(errors);
    }

    public static IServiceResult<T> Forbidden<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.ForbiddenInternal(errors);
    }

    public static IServiceResult ServiceUnavailable(params string[] errors)
    {
        return ServiceResultInternal<Void>.ServiceUnavailableInternal(errors);
    }

    public static IServiceResult<T> ServiceUnavailable<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.ServiceUnavailableInternal(errors);
    }

    public static IServiceResult UnexpectedError(params string[] errors)
    {
        return ServiceResultInternal<Void>.UnexpectedErrorInternal(errors);
    }

    public static IServiceResult<T> UnexpectedError<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.UnexpectedErrorInternal(errors);
    }


    public static IServiceResult NotImplemented(params string[] errors)
    {
        return ServiceResultInternal<Void>.NotImplementedInternal(errors);
    }

    public static IServiceResult<T> NotImplemented<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.NotImplementedInternal(errors);
    }

    public static IServiceResult<T> Conflict<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.ConflictInternal(errors);
    }

    public static IServiceResult Conflict(params string[] errors)
    {
        return ServiceResultInternal<Void>.ConflictInternal(errors);
    }

    public static IServiceResult<T> Unauthorized<T>(params string[] errors)
    {
        return ServiceResultInternal<T>.UnauthorizedInternal(errors);
    }

    public static IServiceResult Unauthorized(params string[] errors)
    {
        return ServiceResultInternal<Void>.UnauthorizedInternal(errors);
    }

    private class ServiceResultInternal<T> : IServiceResult<T>
    {
        private readonly T _payload;

        private ServiceResultInternal(string status, bool isSuccessful)
        {
            Status = status;
            IsSuccessful = isSuccessful;
            Errors = Array.Empty<string>();
        }

        private ServiceResultInternal(string status, T payload)
            : this(status, true)
        {
            _payload = payload;
        }

        private ServiceResultInternal(string status, string[] errors)
            : this(status, false)
        {
            Errors = errors;
        }

        public string Status { get; }
        public bool IsSuccessful { get; }
        public string[] Errors { get; }

        public T Payload => _payload;

        public object PayloadAsObject => _payload;

        public static IServiceResult<T> Internal(string status, bool isSuccessful)
        {
            return new ServiceResultInternal<T>(status, isSuccessful);
        }

        public static IServiceResult<T> Internal(string status, T payload)
        {
            return new ServiceResultInternal<T>(status, payload);
        }

        public static IServiceResult<T> Internal(string status, params string[] errors)
        {
            return new ServiceResultInternal<T>(status, errors);
        }

        public static IServiceResult<T> OkInternal(T payload)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Ok, payload);
        }

        public static IServiceResult<T> CreatedInternal(T payload)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Created, payload);
        }

        public static IServiceResult<T> AcceptedInternal(T payload)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Accepted, payload);
        }

        public static IServiceResult<T> UpdatedInternal(T payload)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Updated, payload);
        }

        public static IServiceResult<T> DeletedInternal(T payload)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Deleted, payload);
        }
        public static IServiceResult<T> IgnoredInternal()
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Ignored, true);
        }

        public static IServiceResult<T> ValidationErrorInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.ValidationError, errors);
        }

        public static IServiceResult<T> NotFoundInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.NotFound, errors);
        }

        public static IServiceResult<T> BadRequestInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.BadRequest, errors);
        }

        public static IServiceResult<T> ForbiddenInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Forbidden, errors);
        }

        public static IServiceResult<T> ServiceUnavailableInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.ServiceUnavailable, errors);
        }

        public static IServiceResult<T> UnexpectedErrorInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.UnexpectedError, errors);
        }

        public static IServiceResult<T> NotImplementedInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.NotImplemented, errors);
        }

        public static IServiceResult<T> ConflictInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Conflict, errors);
        }

        public static IServiceResult<T> UnauthorizedInternal(params string[] errors)
        {
            return new ServiceResultInternal<T>(ServiceResultStatus.Unauthorized, errors);
        }
    }
}

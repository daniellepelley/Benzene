namespace Benzene.Results;

public static class BenzeneResult
{
    public static IBenzeneResult Set(string status)
    {
        return Set(status, new Void());
    }

    public static IBenzeneResult<T> Set<T>(string status, bool isSuccessful)
    {
        return ServiceBenzeneResultInternal<T>.Internal(status, isSuccessful);
    }

    public static IBenzeneResult<T> Set<T>(string status, T payload)
    {
        return ServiceBenzeneResultInternal<T>.Internal(status, payload);
    }

    public static IBenzeneResult<T> Set<T>(string status, params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.Internal(status, errors);
    }

    public static IBenzeneResult Ok()
    {
        return Ok(new Void());
    }

    public static IBenzeneResult<T> Ok<T>()
    {
        return ServiceBenzeneResultInternal<T>.OkInternal(default);
    }
    
    public static IBenzeneResult<T> Ok<T>(T payload)
    {
        return ServiceBenzeneResultInternal<T>.OkInternal(payload);
    }

    public static IBenzeneResult Created()
    {
        return Created(new Void());
    }

    public static IBenzeneResult<T> Created<T>()
    {
        return ServiceBenzeneResultInternal<T>.CreatedInternal(default);
    }

    public static IBenzeneResult<T> Created<T>(T payload)
    {
        return ServiceBenzeneResultInternal<T>.CreatedInternal(payload);
    }

    public static IBenzeneResult Accepted()
    {
        return Accepted(new Void());
    }

    public static IBenzeneResult<T> Accepted<T>()
    {
        return ServiceBenzeneResultInternal<T>.AcceptedInternal(default);
    }

    public static IBenzeneResult<T> Accepted<T>(T payload)
    {
        return ServiceBenzeneResultInternal<T>.AcceptedInternal(payload);
    }

    public static IBenzeneResult Updated()
    {
        return Updated(new Void());
    }

    public static IBenzeneResult<T> Updated<T>()
    {
        return ServiceBenzeneResultInternal<T>.UpdatedInternal(default);
    }

    public static IBenzeneResult<T> Updated<T>(T payload)
    {
        return ServiceBenzeneResultInternal<T>.UpdatedInternal(payload);
    }

    public static IBenzeneResult Deleted()
    {
        return Deleted(new Void());
    }

    public static IBenzeneResult<T> Deleted<T>()
    {
        return ServiceBenzeneResultInternal<T>.DeletedInternal(default);
    }

    public static IBenzeneResult<T> Deleted<T>(T payload)
    {
        return ServiceBenzeneResultInternal<T>.DeletedInternal(payload);
    }

    public static IBenzeneResult Ignored()
    {
        return ServiceBenzeneResultInternal<Void>.IgnoredInternal();
    }

    public static IBenzeneResult<T> Ignored<T>()
    {
        return ServiceBenzeneResultInternal<T>.IgnoredInternal();
    }

    public static IBenzeneResult ValidationError(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.ValidationErrorInternal(errors);
    }

    public static IBenzeneResult<T> ValidationError<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.ValidationErrorInternal(errors);
    }

    public static IBenzeneResult NotFound(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.NotFoundInternal(errors);
    }

    public static IBenzeneResult<T> NotFound<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.NotFoundInternal(errors);
    }
    public static IBenzeneResult BadRequest(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.BadRequestInternal(errors);
    }

    public static IBenzeneResult<T> BadRequest<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.BadRequestInternal(errors);
    }

    public static IBenzeneResult Forbidden(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.ForbiddenInternal(errors);
    }

    public static IBenzeneResult<T> Forbidden<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.ForbiddenInternal(errors);
    }

    public static IBenzeneResult ServiceUnavailable(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.ServiceUnavailableInternal(errors);
    }

    public static IBenzeneResult<T> ServiceUnavailable<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.ServiceUnavailableInternal(errors);
    }

    public static IBenzeneResult UnexpectedError(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.UnexpectedErrorInternal(errors);
    }

    public static IBenzeneResult<T> UnexpectedError<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.UnexpectedErrorInternal(errors);
    }


    public static IBenzeneResult NotImplemented(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.NotImplementedInternal(errors);
    }

    public static IBenzeneResult<T> NotImplemented<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.NotImplementedInternal(errors);
    }

    public static IBenzeneResult<T> Conflict<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.ConflictInternal(errors);
    }

    public static IBenzeneResult Conflict(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.ConflictInternal(errors);
    }

    public static IBenzeneResult<T> Unauthorized<T>(params string[] errors)
    {
        return ServiceBenzeneResultInternal<T>.UnauthorizedInternal(errors);
    }

    public static IBenzeneResult Unauthorized(params string[] errors)
    {
        return ServiceBenzeneResultInternal<Void>.UnauthorizedInternal(errors);
    }

    private class ServiceBenzeneResultInternal<T> : IBenzeneResult<T>
    {
        private readonly T _payload;

        private ServiceBenzeneResultInternal(string status, bool isSuccessful)
        {
            Status = status;
            IsSuccessful = isSuccessful;
            Errors = Array.Empty<string>();
        }

        private ServiceBenzeneResultInternal(string status, T payload)
            : this(status, true)
        {
            _payload = payload;
        }

        private ServiceBenzeneResultInternal(string status, string[] errors)
            : this(status, false)
        {
            Errors = errors;
        }

        public string Status { get; }
        public bool IsSuccessful { get; }
        public string[] Errors { get; }

        public T Payload => _payload;

        public object PayloadAsObject => _payload;

        public static IBenzeneResult<T> Internal(string status, bool isSuccessful)
        {
            return new ServiceBenzeneResultInternal<T>(status, isSuccessful);
        }

        public static IBenzeneResult<T> Internal(string status, T payload)
        {
            return new ServiceBenzeneResultInternal<T>(status, payload);
        }

        public static IBenzeneResult<T> Internal(string status, params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(status, errors);
        }

        public static IBenzeneResult<T> OkInternal(T payload)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Ok, payload);
        }

        public static IBenzeneResult<T> CreatedInternal(T payload)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Created, payload);
        }

        public static IBenzeneResult<T> AcceptedInternal(T payload)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Accepted, payload);
        }

        public static IBenzeneResult<T> UpdatedInternal(T payload)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Updated, payload);
        }

        public static IBenzeneResult<T> DeletedInternal(T payload)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Deleted, payload);
        }
        public static IBenzeneResult<T> IgnoredInternal()
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Ignored, true);
        }

        public static IBenzeneResult<T> ValidationErrorInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.ValidationError, errors);
        }

        public static IBenzeneResult<T> NotFoundInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.NotFound, errors);
        }

        public static IBenzeneResult<T> BadRequestInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.BadRequest, errors);
        }

        public static IBenzeneResult<T> ForbiddenInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Forbidden, errors);
        }

        public static IBenzeneResult<T> ServiceUnavailableInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.ServiceUnavailable, errors);
        }

        public static IBenzeneResult<T> UnexpectedErrorInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.UnexpectedError, errors);
        }

        public static IBenzeneResult<T> NotImplementedInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.NotImplemented, errors);
        }

        public static IBenzeneResult<T> ConflictInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Conflict, errors);
        }

        public static IBenzeneResult<T> UnauthorizedInternal(params string[] errors)
        {
            return new ServiceBenzeneResultInternal<T>(BenzeneResultStatus.Unauthorized, errors);
        }
    }
}

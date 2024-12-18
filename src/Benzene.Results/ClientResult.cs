namespace Benzene.Results;
public static class ClientResult
{
    public static IClientResult Set(string status, bool isSuccessful)
    {
        return Set<Void>(status, isSuccessful);
    }

    public static IClientResult<T> Set<T>(string status, bool isSuccessful)
    {
        return ClientResultInternal<T>.Internal(status, isSuccessful);
    }

    public static IClientResult<T> Set<T>(string status, T payload)
    {
        return ClientResultInternal<T>.Internal(status, payload);
    }

    public static IClientResult<T> Set<T>(string status, params string[] errors)
    {
        return ClientResultInternal<T>.Internal(status, errors);
    }
    public static IClientResult<T> Set<T>(string status, string? error)
    {
        return error == null
            ? ClientResultInternal<T>.Internal(status, false)
            : ClientResultInternal<T>.Internal(status, [error]);
    }


    public static IClientResult Accepted()
    {
        return Accepted(new Void());
    }

    public static IClientResult<T> Accepted<T>()
    {
        return ClientResultInternal<T>.AcceptedInternal(default);
    }

    public static IClientResult<T> Accepted<T>(T payload)
    {
        return ClientResultInternal<T>.AcceptedInternal(payload);
    }

    public static IClientResult Ok()
    {
        return Ok(new Void());
    }

    public static IClientResult<T> Ok<T>()
    {
        return ClientResultInternal<T>.OkInternal(default);
    }

    public static IClientResult<T> Ok<T>(T payload)
    {
        return ClientResultInternal<T>.OkInternal(payload);
    }

    public static IClientResult Created()
    {
        return Created(new Void());
    }

    public static IClientResult<T> Created<T>()
    {
        return ClientResultInternal<T>.CreatedInternal(default);
    }

    public static IClientResult<T> Created<T>(T payload)
    {
        return ClientResultInternal<T>.CreatedInternal(payload);
    }

    public static IClientResult Updated()
    {
        return Updated(new Void());
    }

    public static IClientResult<T> Updated<T>()
    {
        return ClientResultInternal<T>.UpdatedInternal(default);
    }

    public static IClientResult<T> Updated<T>(T payload)
    {
        return ClientResultInternal<T>.UpdatedInternal(payload);
    }

    public static IClientResult Deleted()
    {
        return Deleted(new Void());
    }

    public static IClientResult<T> Deleted<T>()
    {
        return ClientResultInternal<T>.DeletedInternal(default);
    }

    public static IClientResult<T> Deleted<T>(T payload)
    {
        return ClientResultInternal<T>.DeletedInternal(payload);
    }
    
    public static IClientResult Ignored()
    {
        return ClientResultInternal<Void>.IgnoredInternal();
    }

    public static IClientResult<T> Ignored<T>()
    {
        return ClientResultInternal<T>.IgnoredInternal();
    }

    public static IClientResult NotFound(params string[] errors)
    {
        return ClientResultInternal<Void>.NotFoundInternal(errors);
    }

    public static IClientResult<T> NotFound<T>(params string[] errors)
    {
        return ClientResultInternal<T>.NotFoundInternal(errors);
    }
    
    public static IClientResult BadRequest(params string[] errors)
    {
        return ClientResultInternal<Void>.BadRequestInternal(errors);
    }

    public static IClientResult<T> BadRequest<T>(params string[] errors)
    {
        return ClientResultInternal<T>.BadRequestInternal(errors);
    }

    public static IClientResult ValidationError(params string[] errors)
    {
        return ClientResultInternal<Void>.ValidationErrorInternal(errors);
    }

    public static IClientResult<T> ValidationError<T>(params string[] errors)
    {
        return ClientResultInternal<T>.ValidationErrorInternal(errors);
    }

    public static IClientResult Conflict(params string[] errors)
    {
        return ClientResultInternal<Void>.ConflictInternal(errors);
    }

    public static IClientResult<T> Conflict<T>(params string[] errors)
    {
        return ClientResultInternal<T>.ConflictInternal(errors);
    }

    public static IClientResult Forbidden(params string[] errors)
    {
        return ClientResultInternal<Void>.ForbiddenInternal(errors);
    }

    public static IClientResult<T> Forbidden<T>(params string[] errors)
    {
        return ClientResultInternal<T>.ForbiddenInternal(errors);
    }

    public static IClientResult Unauthorized(params string[] errors)
    {
        return ClientResultInternal<Void>.UnauthorizedInternal(errors);
    }

    public static IClientResult<T> Unauthorized<T>(params string[] errors)
    {
        return ClientResultInternal<T>.UnauthorizedInternal(errors);
    }

    public static IClientResult ServiceUnavailable(params string[] errors)
    {
        return ClientResultInternal<Void>.ServiceUnavailableInternal(errors);
    }

    public static IClientResult<T> ServiceUnavailable<T>(params string[] errors)
    {
        return ClientResultInternal<T>.ServiceUnavailableInternal(errors);
    }
    
    public static IClientResult NotImplemented(params string[] errors)
    {
        return ClientResultInternal<Void>.NotImplementedInternal(errors);
    }

    public static IClientResult<T> NotImplemented<T>(params string[] errors)
    {
        return ClientResultInternal<T>.NotImplementedInternal(errors);
    }

    public static IClientResult UnexpectedError(params string[] errors)
    {
        return ClientResultInternal<Void>.UnexpectedErrorInternal(errors);
    }

    public static IClientResult<T> UnexpectedError<T>(params string[] errors)
    {
        return ClientResultInternal<T>.UnexpectedErrorInternal(errors);
    }
        
    private class ClientResultInternal<T> : IClientResult<T>
    {
        public string Status { get; }
        public bool IsSuccessful { get; }
        public string[] Errors { get; }

        private readonly T _payload;

        public T Payload => _payload;

        public object PayloadAsObject => _payload;

        private ClientResultInternal(string status, bool isSuccessful)
        {
            Status = status;
            IsSuccessful = isSuccessful;
            Errors = Array.Empty<string>();
        }

        private ClientResultInternal(string status, T payload)
            : this(status, true)
        {
            _payload = payload;
        }

        private ClientResultInternal(string status, string[] errors)
            : this(status, false)
        {
            Errors = errors;
        }
        public static IClientResult<T> Internal(string status, bool isSuccessful)
        {
            return new ClientResultInternal<T>(status, isSuccessful);
        }

        public static IClientResult<T> Internal(string status, T payload)
        {
            return new ClientResultInternal<T>(status, payload);
        }

        public static IClientResult<T> Internal(string status, params string[] errors)
        {
            return new ClientResultInternal<T>(status, errors);
        }

        public static IClientResult<T> AcceptedInternal(T payload)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Accepted, payload);
        }

        public static IClientResult<T> OkInternal(T payload)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Ok, payload);
        }

        public static IClientResult<T> CreatedInternal(T payload)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Created, payload);
        }

        public static IClientResult<T> UpdatedInternal(T payload)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Updated, payload);
        }

        public static IClientResult<T> DeletedInternal(T payload)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Deleted, payload);
        }
        
        public static IClientResult<T> IgnoredInternal()
        {
            return new ClientResultInternal<T>(ClientResultStatus.Ignored, true);
        }

        public static IClientResult<T> NotFoundInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.NotFound, errors);
        }

        public static IClientResult<T> BadRequestInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.BadRequest, errors);
        }


        public static IClientResult<T> ValidationErrorInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.ValidationError, errors);
        }


        public static IClientResult<T> ConflictInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Conflict, errors);
        }

        public static IClientResult<T> ForbiddenInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Forbidden, errors);
        }

        public static IClientResult<T> UnauthorizedInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.Unauthorized, errors);
        }

        public static IClientResult<T> ServiceUnavailableInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.ServiceUnavailable, errors);
        }
        
        public static IClientResult<T> NotImplementedInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.NotImplemented, errors);
        }

        public static IClientResult<T> UnexpectedErrorInternal(params string[] errors)
        {
            return new ClientResultInternal<T>(ClientResultStatus.UnexpectedError, errors);
        }
    }

}

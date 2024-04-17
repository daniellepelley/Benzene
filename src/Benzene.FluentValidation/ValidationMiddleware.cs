using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.FluentValidation;

public class ValidationMiddleware<TRequest, TResponse> : IMiddleware<IMessageContext<TRequest, TResponse>> 
    where TRequest : class
{
    private readonly IServiceResolver _serviceResolver;

    public ValidationMiddleware(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public string Name => "FluentValidation";

    public async Task HandleAsync(IMessageContext<TRequest, TResponse> context, Func<Task> next)
    {
        var validator = _serviceResolver.TryGetService<IValidator<TRequest>>();
        if (validator != null)
        {
            if (context.Request == default)
            {
                context.Response = ServiceResult.ValidationError<TResponse>("Request is null");
                return;
            }
                
            var validationResult = await validator.ValidateAsync(context.Request);
            if (!validationResult.IsValid)
            {
                context.Response =
                    ServiceResult.ValidationError<TResponse>(validationResult.Errors.Select(x => x.ErrorMessage)
                        .ToArray());
                return;
            }
        }
        await next();
    }
}

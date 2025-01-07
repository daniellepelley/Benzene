using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.FluentValidation;

public class ValidationMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>> 
    where TRequest : class
{
    private readonly IServiceResolver _serviceResolver;

    public ValidationMiddleware(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public string Name => "FluentValidation";

    public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
    {
        var validator = _serviceResolver.TryGetService<IValidator<TRequest>>();
        if (validator != null)
        {
            if (context.Request == default)
            {
                context.Response = BenzeneResult.ValidationError<TResponse>("Request is null");
                return;
            }
                
            var validationResult = await validator.ValidateAsync(context.Request);
            if (!validationResult.IsValid)
            {
                context.Response =
                    BenzeneResult.ValidationError<TResponse>(validationResult.Errors.Select(x => x.ErrorMessage)
                        .ToArray());
                return;
            }
        }
        await next();
    }
}
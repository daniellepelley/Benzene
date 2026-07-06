using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Validation;
using Benzene.Results;

namespace Benzene.FluentValidation;

public class ValidationMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>> 
    where TRequest : class
{
    private readonly IServiceResolver _serviceResolver;
    private readonly IValidationStatusMapper _validationStatusMapper;

    public ValidationMiddleware(IServiceResolver serviceResolver, IValidationStatusMapper validationStatusMapper)
    {
        _serviceResolver = serviceResolver;
        _validationStatusMapper = validationStatusMapper;
    }

    public string Name => "FluentValidation";

    public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
    {
        var validator = _serviceResolver.TryGetService<IValidator<TRequest>>();
        if (validator != null)
        {
            if (context.Request == default)
            {
                var status = _validationStatusMapper.GetStatus(context.HandlerType, typeof(TRequest), null);
                context.Response = BenzeneResult.Set<TResponse>(status, "Request is null");
                return;
            }
                
            var validationResult = await validator.ValidateAsync(context.Request);
            if (!validationResult.IsValid)
            {
                var status = _validationStatusMapper.GetStatus(context.HandlerType, typeof(TRequest), validationResult);
                context.Response =
                    BenzeneResult.Set<TResponse>(status, validationResult.Errors.Select(x => x.ErrorMessage)
                        .ToArray());
                return;
            }
        }
        await next();
    }
}
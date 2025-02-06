using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Results;
using FluentValidation;

namespace Benzene.FluentValidation;

public class ValidationClientMiddleware<TRequest, TResponse> : IMiddleware<IBenzeneClientContext<TRequest, TResponse>>
    where TRequest : class
{
    private readonly IServiceResolver _serviceResolver;

    public ValidationClientMiddleware(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public string Name => "FluentValidation";

    public async Task HandleAsync(IBenzeneClientContext<TRequest, TResponse> context, Func<Task> next)
    {
        var validator = _serviceResolver.TryGetService<IValidator<TRequest>>();
        if (validator != null)
        {
            if (context.Request.Message == default)
            {
                context.Response = BenzeneResult.ValidationError<TResponse>("Request is null");
                return;
            }

            var validationResult = await validator.ValidateAsync(context.Request.Message);
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
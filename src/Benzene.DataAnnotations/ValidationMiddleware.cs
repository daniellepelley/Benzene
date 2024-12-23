using System.ComponentModel.DataAnnotations;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.DataAnnotations;

public class ValidationMiddleware<TRequest, TResponse> : IMiddleware<IMessageContext<TRequest, TResponse>>
    where TRequest : class
{
    public string Name => "DataAnnotationValidation";

    public async Task HandleAsync(IMessageContext<TRequest, TResponse> context, Func<Task> next)
    {
        if (context.Request == default)
        {
            context.Response = BenzeneResult.ValidationError<TResponse>("Request is null");
            return;
        }

        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(context.Request, null, null);
        Validator.TryValidateObject(context.Request, ctx, validationResults, true);
        if (validationResults.Any())
        {
            context.Response =
                BenzeneResult.ValidationError<TResponse>(validationResults
                    .Where(x => x.ErrorMessage != null)
                    .Select(x => x.ErrorMessage!)
                    .ToArray());
            return;
        }
        await next();
    }
}

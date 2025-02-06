using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;

namespace Benzene.FluentValidation;

public class ValidationClientMiddlewareBuilder : IBenzeneClientContextMiddlewareBuilder
{
    public IMiddleware<IBenzeneClientContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver) where TRequest : class
    {
        return new ValidationClientMiddleware<TRequest, TResponse>(serviceResolver);
    }
}
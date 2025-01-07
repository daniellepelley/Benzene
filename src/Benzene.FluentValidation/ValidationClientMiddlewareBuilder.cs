using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;

namespace Benzene.FluentValidation;

public class ValidationClientMiddlewareBuilder : IBenzeneClientContextMiddlewareBuilder
{
    public IMiddleware<IBenzeneClientContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver) where TRequest : class
    {
        return new ValidationClientMiddleware<TRequest, TResponse>(serviceResolver);
    }
}
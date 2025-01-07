using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware.BenzeneClient;

public interface IBenzeneClientContextMiddlewareBuilder
{
    IMiddleware<IBenzeneClientContext<TRequest, TResponse>>? Create<TRequest, TResponse>(IServiceResolver serviceResolver)
        where TRequest : class;
}

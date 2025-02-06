using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.Messages.BenzeneClient;

public interface IBenzeneClientContextMiddlewareBuilder
{
    IMiddleware<IBenzeneClientContext<TRequest, TResponse>>? Create<TRequest, TResponse>(IServiceResolver serviceResolver)
        where TRequest : class;
}

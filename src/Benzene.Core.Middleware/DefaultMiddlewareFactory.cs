using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class DefaultMiddlewareFactory(IEnumerable<IMiddlewareWrapper> middlewareWrappers) : IMiddlewareFactory
{
    public IMiddleware<TContext> Create<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return middlewareWrappers.Aggregate(middleware, (m, wrapper) => wrapper.Wrap(serviceResolver, m));
    }
}
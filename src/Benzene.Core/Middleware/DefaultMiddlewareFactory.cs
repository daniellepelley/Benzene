using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class DefaultMiddlewareFactory : IMiddlewareFactory
{
    private readonly IEnumerable<IMiddlewareWrapper> _middlewareWrappers;

    public DefaultMiddlewareFactory(IEnumerable<IMiddlewareWrapper> middlewareWrappers)
    {
        _middlewareWrappers = middlewareWrappers;
    }

    public IMiddleware<TContext> Create<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return _middlewareWrappers.Aggregate(middleware, (m, wrapper) => wrapper.Wrap(serviceResolver, m));
    }
}
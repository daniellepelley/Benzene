using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Diagnostics;

public class DebugMiddlewareWrapper : IMiddlewareWrapper
{
    public IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return new DebugMiddlewareDecorator<TContext>(middleware);
    }
}
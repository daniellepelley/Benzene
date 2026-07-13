using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Diagnostics;

/// <summary>
/// Wraps every middleware in the pipeline in an <see cref="ActivityMiddlewareDecorator{TContext}"/>,
/// starting an <see cref="System.Diagnostics.Activity"/> per pipeline stage. Registered by
/// <see cref="DependencyInjectionExtensions.AddDiagnostics"/>.
/// </summary>
public class ActivityMiddlewareWrapper : IMiddlewareWrapper
{
    public IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return new ActivityMiddlewareDecorator<TContext>(middleware, serviceResolver);
    }
}

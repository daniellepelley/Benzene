using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides the default implementation of <see cref="IMiddlewareFactory"/> that applies middleware wrappers
/// to middleware instances.
/// </summary>
/// <remarks>
/// This factory uses registered middleware wrappers to decorate middleware instances, allowing for
/// cross-cutting concerns like logging, timing, or error handling to be applied consistently across
/// all middleware components in the pipeline.
/// </remarks>
public class DefaultMiddlewareFactory(IEnumerable<IMiddlewareWrapper> middlewareWrappers) : IMiddlewareFactory
{
    /// <summary>
    /// Creates a middleware instance by applying all registered wrappers.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="middleware">The base middleware instance to wrap.</param>
    /// <returns>The middleware instance with all wrappers applied.</returns>
    public IMiddleware<TContext> Create<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return middlewareWrappers.Aggregate(middleware, (m, wrapper) => wrapper.Wrap(serviceResolver, m));
    }
}
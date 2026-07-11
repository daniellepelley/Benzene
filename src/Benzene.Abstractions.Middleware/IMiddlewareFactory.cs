using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Provides a factory for creating middleware instances with dependency injection support.
/// </summary>
/// <remarks>
/// Middleware factories enable advanced scenarios such as:
/// - Wrapping middleware with additional behavior (logging, metrics, exception handling)
/// - Lazy initialization of middleware with dependencies
/// - Dynamic middleware composition based on runtime conditions
/// - Integration with dependency injection containers
/// </remarks>
public interface IMiddlewareFactory
{
    /// <summary>
    /// Creates a new middleware instance, potentially wrapping or enhancing the provided middleware.
    /// </summary>
    /// <typeparam name="TContext">The type of context object the middleware processes.</typeparam>
    /// <param name="serviceResolver">The service resolver for resolving dependencies required by the middleware.</param>
    /// <param name="middleware">The base middleware instance to create or wrap.</param>
    /// <returns>A middleware instance, which may be the original middleware or a wrapped/enhanced version.</returns>
    /// <remarks>
    /// Implementations typically use this method to:
    /// - Apply cross-cutting concerns (logging, telemetry, etc.)
    /// - Resolve constructor dependencies for middleware
    /// - Apply decorators or wrappers to existing middleware
    /// </remarks>
    IMiddleware<TContext> Create<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware);
}
using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Provides functionality to wrap middleware instances with additional behavior.
/// </summary>
/// <remarks>
/// Middleware wrappers implement the decorator pattern to add cross-cutting concerns to existing middleware without modifying them.
/// Common use cases include:
/// - Exception handling and error recovery
/// - Logging and diagnostics
/// - Performance monitoring and metrics
/// - Request/response transformation
/// - Security and authorization checks
/// </remarks>
public interface IMiddlewareWrapper
{
    /// <summary>
    /// Wraps the specified middleware with additional functionality.
    /// </summary>
    /// <typeparam name="TContext">The type of context object the middleware processes.</typeparam>
    /// <param name="serviceResolver">The service resolver for resolving dependencies required by the wrapper.</param>
    /// <param name="middleware">The middleware instance to wrap.</param>
    /// <returns>A new middleware instance that wraps the original middleware with additional behavior.</returns>
    /// <remarks>
    /// The returned middleware should invoke the wrapped middleware at the appropriate point in its execution,
    /// potentially adding behavior before and/or after the wrapped middleware executes.
    /// </remarks>
    IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware);
}
namespace Benzene.Core.MessageHandlers.Filters;

/// <summary>
/// Allows an already-routed request to be conditionally rejected before it reaches its handler, e.g.
/// for feature flags, tenant gating, or short-circuiting messages a handler shouldn't process.
/// </summary>
/// <typeparam name="T">The strongly-typed request this filter inspects.</typeparam>
/// <remarks>
/// Registered via the <c>AddFilters</c> extension methods on <see cref="DependencyExtensions"/>
/// and applied by <see cref="FiltersMiddleware{TRequest,TResponse}"/>, which resolves at most one
/// <see cref="IFilter{T}"/> per request type from DI. If <see cref="Filter"/> returns <c>false</c>,
/// the handler is never invoked and the response is set to an "ignored" result instead.
/// </remarks>
public interface IFilter<T>
{
    /// <summary>
    /// Decides whether the given request should be allowed to proceed to its handler.
    /// </summary>
    /// <param name="value">The strongly-typed request to inspect.</param>
    /// <returns><c>true</c> if the request should be handled; <c>false</c> to short-circuit it as "ignored".</returns>
    bool Filter(T value);
}

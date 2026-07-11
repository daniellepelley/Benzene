using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Provides conditional logic for determining whether middleware should execute based on context state.
/// </summary>
/// <typeparam name="TContext">The type of context to evaluate.</typeparam>
/// <remarks>
/// Context predicates enable conditional middleware execution and pipeline branching based on runtime conditions.
/// Common use cases include:
/// - Routing middleware based on request properties (HTTP method, path, headers)
/// - Conditional middleware execution (skip certain middleware for specific requests)
/// - Circuit breaker patterns (bypass middleware based on error state)
/// - Feature flags and A/B testing (conditional pipeline behavior)
/// - Multi-tenant routing (different pipelines per tenant)
/// Predicates can access the dependency injection container to resolve configuration or services needed for evaluation.
/// </remarks>
public interface IContextPredicate<TContext>
{
    /// <summary>
    /// Checks whether the predicate condition is satisfied for the given context.
    /// </summary>
    /// <param name="context">The context to evaluate.</param>
    /// <param name="serviceResolver">The service resolver for accessing dependencies needed during evaluation.</param>
    /// <returns><c>true</c> if the predicate condition is met; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Implementations should:
    /// - Evaluate the context against specific criteria
    /// - Return a boolean indicating whether the condition is satisfied
    /// - Be deterministic and side-effect free when possible
    /// - Use the service resolver sparingly to avoid performance overhead
    /// The result typically controls whether middleware executes or whether a particular pipeline branch is taken.
    /// </remarks>
    bool Check(TContext context, IServiceResolver serviceResolver);
}

using Benzene.Abstractions.DI;

namespace Benzene.Abstractions;

/// <summary>
/// Provides a mechanism to wrap dependencies with additional behavior or decorators.
/// This interface enables the decorator pattern for cross-cutting concerns in the middleware pipeline.
/// </summary>
/// <typeparam name="T">The type of dependency to wrap.</typeparam>
/// <remarks>
/// Superseded, for its one real consumer (<c>Benzene.Clients</c>'s outbound decorator chain), by
/// ordinary outbound middleware on an <c>OutboundRoutingBuilder.Route</c> pipeline - the framework's
/// own middleware pipeline replaces this parallel decorator mechanism. See
/// <c>work/benzene-clients-redesign-plan.md</c> §2.4.
/// </remarks>
[Obsolete("Use ordinary IMiddleware<TContext> outbound middleware instead - see work/benzene-clients-redesign-plan.md")]
public interface IDependencyWrapper<T>
{
    /// <summary>
    /// Wraps the source dependency with additional behavior.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for accessing additional dependencies.</param>
    /// <param name="source">The source dependency to wrap.</param>
    /// <returns>The wrapped dependency.</returns>
    T Wrap(IServiceResolver serviceResolver, T source);
}

using Benzene.Abstractions.DI;

namespace Benzene.Abstractions;

/// <summary>
/// Provides a mechanism to wrap dependencies with additional behavior or decorators.
/// This interface enables the decorator pattern for cross-cutting concerns in the middleware pipeline.
/// </summary>
/// <typeparam name="T">The type of dependency to wrap.</typeparam>
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

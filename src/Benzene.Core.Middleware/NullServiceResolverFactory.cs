using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides a null object implementation of <see cref="IServiceResolverFactory"/> for testing or scenarios
/// where service resolution is not needed.
/// </summary>
/// <remarks>
/// This implementation follows the Null Object pattern, creating <see cref="NullServiceResolver"/> instances
/// when scopes are requested. This prevents null reference exceptions when a service resolver factory is
/// required but no actual service resolution needs to occur.
/// </remarks>
public class NullServiceResolverFactory : IServiceResolverFactory
{
    /// <summary>
    /// Disposes the service resolver factory.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Creates a new service resolver scope.
    /// </summary>
    /// <returns>A null service resolver instance.</returns>
    public IServiceResolver CreateScope()
    {
        return new NullServiceResolver();

    }
}
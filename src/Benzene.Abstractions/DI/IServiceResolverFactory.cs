namespace Benzene.Abstractions.DI;

/// <summary>
/// Provides a factory for creating service resolver scopes.
/// The factory maintains the root container and creates child scopes for scoped service resolution.
/// </summary>
public interface IServiceResolverFactory : IDisposable
{
    /// <summary>
    /// Creates a new service resolver scope.
    /// </summary>
    /// <returns>A new service resolver representing a DI scope.</returns>
    IServiceResolver CreateScope();
}
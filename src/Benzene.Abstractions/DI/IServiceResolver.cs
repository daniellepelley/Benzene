namespace Benzene.Abstractions.DI;

/// <summary>
/// Provides scoped resolution of services from the DI container.
/// Each resolver represents a dependency injection scope and should be disposed when the scope ends.
/// </summary>
public interface IServiceResolver : IDisposable
{
    /// <summary>
    /// Gets a required service from the container.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    T GetService<T>() where T : class;

    /// <summary>
    /// Attempts to get a service from the container.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance if registered; otherwise, null.</returns>
    T? TryGetService<T>() where T : class;
}
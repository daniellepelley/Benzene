namespace Benzene.Abstractions.DI;

/// <summary>
/// Provides convenience extension methods for IServiceResolver.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Resolves a required service from the container. Alias for GetService.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <param name="source">The service resolver.</param>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    public static T Resolve<T>(this IServiceResolver source) where T : class
    {
        return source.GetService<T>();
    }

    /// <summary>
    /// Attempts to resolve a service from the container. Alias for TryGetService.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <param name="source">The service resolver.</param>
    /// <returns>The resolved service instance if registered; otherwise, null.</returns>
    public static T? TryResolve<T>(this IServiceResolver source) where T : class
    {
        return source.TryGetService<T>();
    }
}

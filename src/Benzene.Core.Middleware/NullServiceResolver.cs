using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides a null object implementation of <see cref="IServiceResolver"/> for testing or scenarios
/// where service resolution is not needed.
/// </summary>
/// <remarks>
/// This implementation follows the Null Object pattern, returning null or default values for all
/// service resolution requests. This prevents null reference exceptions when a service resolver is
/// required but no actual services need to be resolved.
/// </remarks>
public class NullServiceResolver : IServiceResolver
{
    /// <summary>
    /// Disposes the service resolver.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>Always returns the default value for the type.</returns>
    public T GetService<T>() where T : class
    {
        return default!;
    }

    /// <summary>
    /// Attempts to get a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>Always returns null.</returns>
    public T? TryGetService<T>() where T : class
    {
        return null;
    }

    /// <summary>
    /// Gets all services of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>Always returns an empty sequence.</returns>
    public IEnumerable<T> GetServices<T>() where T : class
    {
        return [];
    }
}
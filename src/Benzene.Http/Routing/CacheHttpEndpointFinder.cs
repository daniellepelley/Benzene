namespace Benzene.Http.Routing;

/// <summary>
/// Provides a caching decorator for <see cref="IHttpEndpointFinder"/> that caches endpoint definitions
/// to avoid repeated discovery operations.
/// </summary>
/// <remarks>
/// This finder wraps another endpoint finder and caches its results. Subsequent calls to
/// <see cref="FindDefinitions"/> return the cached results rather than performing discovery again.
/// This improves performance when endpoint discovery is expensive (e.g., reflection-based discovery).
/// </remarks>
public class CacheHttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IHttpEndpointFinder _inner;
    private IHttpEndpointDefinition[]? _httpEndpointDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheHttpEndpointFinder"/> class.
    /// </summary>
    /// <param name="inner">The underlying endpoint finder to wrap with caching.</param>
    public CacheHttpEndpointFinder(IHttpEndpointFinder inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Finds and returns all HTTP endpoint definitions, using cached results if available.
    /// </summary>
    /// <returns>
    /// An array of HTTP endpoint definitions, either from cache or by calling the inner finder
    /// on the first invocation.
    /// </returns>
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _httpEndpointDefinitions ??= _inner.FindDefinitions();
    }
}

namespace Benzene.Http.Routing;

/// <summary>
/// Discovers HTTP endpoints from the dependency injection container by resolving
/// <see cref="IHttpEndpointDefinition"/> instances.
/// </summary>
/// <remarks>
/// This finder receives endpoint definitions through constructor injection, allowing
/// endpoints to be registered directly in the DI container. This is useful when endpoints
/// are configured programmatically or loaded from external sources at startup.
/// </remarks>
public class DependencyHttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IEnumerable<IHttpEndpointDefinition> _httpEndpointDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyHttpEndpointFinder"/> class.
    /// </summary>
    /// <param name="httpEndpointDefinitions">
    /// The endpoint definitions resolved from the dependency injection container.
    /// </param>
    public DependencyHttpEndpointFinder(IEnumerable<IHttpEndpointDefinition> httpEndpointDefinitions)
    {
        _httpEndpointDefinitions = httpEndpointDefinitions;
    }

    /// <summary>
    /// Finds and returns all HTTP endpoint definitions from the dependency injection container.
    /// </summary>
    /// <returns>An array of endpoint definitions resolved from the DI container.</returns>
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _httpEndpointDefinitions.ToArray();
    }

}
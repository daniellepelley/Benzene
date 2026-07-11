namespace Benzene.Http.Routing;

/// <summary>
/// Provides a list-based endpoint finder that allows manual registration of HTTP endpoints.
/// </summary>
/// <remarks>
/// This finder maintains an internal list of endpoint definitions that can be populated
/// programmatically using the <see cref="Add"/> method. It implements both
/// <see cref="IHttpEndpointFinder"/> for discovery and <see cref="IListHttpEndpointFinder"/>
/// for registration, making it useful for scenarios where endpoints are defined dynamically
/// or loaded from configuration.
/// </remarks>
public class ListHttpEndpointFinder : IHttpEndpointFinder, IListHttpEndpointFinder
{
    private readonly List<IHttpEndpointDefinition> _list = new();

    /// <summary>
    /// Finds and returns all HTTP endpoint definitions that have been manually added to the list.
    /// </summary>
    /// <returns>An array of all endpoint definitions in the list.</returns>
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _list.ToArray();
    }

    /// <summary>
    /// Adds an HTTP endpoint definition to the list.
    /// </summary>
    /// <param name="httpEndpointDefinition">The endpoint definition to add.</param>
    public void Add(IHttpEndpointDefinition httpEndpointDefinition)
    {
        _list.Add(httpEndpointDefinition);
    }
}
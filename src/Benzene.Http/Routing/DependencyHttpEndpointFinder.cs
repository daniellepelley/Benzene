namespace Benzene.Http.Routing;

public class DependencyHttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IEnumerable<IHttpEndpointDefinition> _httpEndpointDefinitions;

    public DependencyHttpEndpointFinder(IEnumerable<IHttpEndpointDefinition> httpEndpointDefinitions)
    {
        _httpEndpointDefinitions = httpEndpointDefinitions;
    }
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _httpEndpointDefinitions.ToArray();
    }

}
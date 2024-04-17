namespace Benzene.Http;

public class CacheHttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IHttpEndpointFinder _inner;
    private IHttpEndpointDefinition[]? _httpEndpointDefinitions;

    public CacheHttpEndpointFinder(IHttpEndpointFinder inner)
    {
        _inner = inner;
    }

    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _httpEndpointDefinitions ??= _inner.FindDefinitions();
    }
}

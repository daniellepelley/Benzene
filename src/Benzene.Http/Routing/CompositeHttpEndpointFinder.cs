namespace Benzene.Http.Routing;

public class CompositeHttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IHttpEndpointFinder[] _inners;

    public CompositeHttpEndpointFinder(params IHttpEndpointFinder[] inners)
    {
        _inners = inners;
    }

    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _inners.SelectMany(x => x.FindDefinitions()).ToArray();
    }
}

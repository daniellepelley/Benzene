namespace Benzene.Http.Routing;


public class ListHttpEndpointFinder : IHttpEndpointFinder, IListHttpEndpointFinder
{
    private readonly List<IHttpEndpointDefinition> _list = new();

    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _list.ToArray();
    }

    public void Add(IHttpEndpointDefinition httpEndpointDefinition)
    {
        _list.Add(httpEndpointDefinition);
    }
}
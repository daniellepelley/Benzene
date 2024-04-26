namespace Benzene.Http.Routing;

public interface IListHttpEndpointFinder
{
    void Add(IHttpEndpointDefinition httpEndpointDefinition);
}
namespace Benzene.Http.Routing;

public interface IHttpEndpointFinder
{
    IHttpEndpointDefinition[] FindDefinitions();
}
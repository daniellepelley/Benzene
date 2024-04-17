namespace Benzene.Http;

public interface IHttpEndpointFinder
{
    IHttpEndpointDefinition[] FindDefinitions();
}
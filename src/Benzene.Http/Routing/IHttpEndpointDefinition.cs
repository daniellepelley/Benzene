namespace Benzene.Http.Routing;

public interface IHttpEndpointDefinition
{
    string Method { get; }
    string Path { get; }
    string Topic { get; }
}
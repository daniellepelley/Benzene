namespace Benzene.Http;

public class HttpEndpointDefinition : IHttpEndpointDefinition
{
    public HttpEndpointDefinition(string method, string path, string topic)
    {
        Method = method;
        Path = path;
        Topic = topic;
    }
    public static IHttpEndpointDefinition CreateInstance(string method, string path, string topic)
    {
        return new HttpEndpointDefinition(method, path, topic);
    }

    public string Method { get; }
    public string Path { get; }
    public string Topic { get; }
}

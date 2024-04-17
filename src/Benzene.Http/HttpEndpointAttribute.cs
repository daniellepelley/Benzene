namespace Benzene.Http;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class HttpEndpointAttribute : Attribute
{
    public HttpEndpointAttribute(string method, string url)
    {
        Url = url;
        Method = method;
    }

    public string Method { get; }
    public string Url { get; }
}
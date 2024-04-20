using Benzene.Abstractions;

namespace Benzene.Tools;

public class HttpBuilder : IHttpBuilder
{
    public IDictionary<string, string> Headers { get; private set; }
    public string Method { get; }
    public string Path { get; }
    public object? Message { get; }

    private HttpBuilder(string method, string path, object? message)
    {
        Message = message;
        Method = method;
        Path = path;
        Headers = new Dictionary<string, string>
        {
            { "x-correlation-id", Guid.NewGuid().ToString() }
        };
    }

    public static HttpBuilder Create(string method, string path, object? message = null)
    {
        return new HttpBuilder(method, path, message);
    }

    public HttpBuilder WithHeaders(IDictionary<string, string> headers)
    {
        Headers = headers;
        return this;
    }

    public HttpBuilder WithHeader(string key, string value)
    {
        Headers.Add(key, value);
        return this;
    }
}

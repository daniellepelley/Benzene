using Benzene.Abstractions;

namespace Benzene.Tools;

public class HttpBuilder<T> : IHttpBuilder<T>
{
    public IDictionary<string, string> Headers { get; private set; }
    public string Method { get; }
    public string Path { get; }
    public T? Message { get; }

    internal HttpBuilder(string method, string path, T? message)
    {
        Message = message;
        Method = method;
        Path = path;
        Headers = new Dictionary<string, string>();
    }

    public static HttpBuilder<T> Create(string method, string path, T? message = default)
    {
        return new HttpBuilder<T>(method, path, message);
    }


    public HttpBuilder<T> WithHeaders(IDictionary<string, string> headers)
    {
        Headers = headers;
        return this;
    }

    public HttpBuilder<T> WithHeader(string key, string value)
    {
        Headers.Add(key, value);
        return this;
    }
}

public static class HttpBuilder 
{
    public static HttpBuilder<object> Create(string method, string path)
    {
        return new HttpBuilder<object>(method, path, null);
    }
    
    public static HttpBuilder<T> Create<T>(string method, string path, T? message = default)
    {
        return new HttpBuilder<T>(method, path, message);
    }
}

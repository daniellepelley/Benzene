using System.Text;
using Newtonsoft.Json;

namespace Benzene.Example.Asp.Test.Helpers;

public class RequestBuilder
{
    private readonly IDictionary<string, string> _headers = new Dictionary<string, string>();
    private readonly HttpMethod _httpMethod;
    private readonly string _path;
    private string _body;

    public RequestBuilder(HttpMethod httpMethod, string path)
    {
        _path = path;
        _httpMethod = httpMethod;
        _headers.Add("x-correlation-id", Guid.NewGuid().ToString());
    }

    public HttpRequestMessage Build()
    {
        return new HttpRequestMessage
        {
            Method = _httpMethod,
            RequestUri = new Uri(_path, UriKind.Relative),
            Content = new StringContent(_body, Encoding.UTF8, "application/json")
        };
    }

    public RequestBuilder WithBody(object body)
    {
        _body = JsonConvert.SerializeObject(body);
        return this;
    }

    public RequestBuilder WithXmlBody<T>(T message)
    {
        _body = XmlHelper.ToXml(message);
        return this;
    }

    public RequestBuilder WithHeader(string key, string value)
    {
        _headers.Add(key, value);
        return this;
    }
}
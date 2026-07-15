using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Benzene.GoogleCloud.Functions.Http.TestHelpers;

/// <summary>
/// Builds a <see cref="DefaultHttpContext"/> for tests that dispatch directly into an
/// <c>IHttpFunction</c> via <see cref="BenzeneTestHostExtensions.SendHttpAsync"/>, without a live
/// Kestrel server. Promotes/generalizes the builder originally hand-rolled in
/// <c>examples/Google/Benzene.Examples.Google.Tests</c>.
/// </summary>
public class HttpContextBuilder
{
    private readonly IDictionary<string, string> _headers = new Dictionary<string, string>();
    private readonly string _httpMethod;
    private readonly string _path;
    private string _body = string.Empty;

    /// <summary>Initializes a new instance targeting the given method and path.</summary>
    /// <param name="httpMethod">The HTTP method (e.g. <c>"GET"</c>, <c>"POST"</c>).</param>
    /// <param name="path">The request path.</param>
    public HttpContextBuilder(string httpMethod, string path)
    {
        _httpMethod = httpMethod;
        _path = path;
    }

    /// <summary>Serializes <paramref name="message"/> as JSON and uses it as the request body.</summary>
    /// <param name="message">The object to serialize as the request body.</param>
    /// <returns>This instance, for method chaining.</returns>
    public HttpContextBuilder WithBody(object message)
    {
        _body = JsonSerializer.Serialize(message);
        return this;
    }

    /// <summary>Uses <paramref name="body"/> verbatim as the request body.</summary>
    /// <param name="body">The raw request body.</param>
    /// <returns>This instance, for method chaining.</returns>
    public HttpContextBuilder WithRawBody(string body)
    {
        _body = body;
        return this;
    }

    /// <summary>Adds a request header.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>This instance, for method chaining.</returns>
    public HttpContextBuilder WithHeader(string key, string value)
    {
        _headers[key] = value;
        return this;
    }

    /// <summary>Builds the <see cref="HttpContext"/>.</summary>
    /// <returns>A <see cref="DefaultHttpContext"/> with the configured request and a writable response body.</returns>
    public HttpContext Build()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = _httpMethod,
                Path = new PathString(_path),
                Body = new MemoryStream(Encoding.UTF8.GetBytes(_body))
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };

        foreach (var header in _headers)
        {
            context.Request.Headers[header.Key] = new StringValues(header.Value);
        }

        return context;
    }
}

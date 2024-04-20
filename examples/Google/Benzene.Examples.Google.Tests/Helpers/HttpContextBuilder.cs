using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Benzene.Examples.Google.Tests.Helpers;

public class HttpContextBuilder
{
    private readonly IDictionary<string, string> _headers = new Dictionary<string, string>();
    private readonly string _httpMethod;
    private readonly string _path;
    private readonly IDictionary<string, string> _pathParameters = new Dictionary<string, string>();
    private string _body = "{}";

    public HttpContextBuilder(string httpMethod, string path)
    {
        _path = path;
        _httpMethod = httpMethod;
        _headers.Add("x-correlation-id", Guid.NewGuid().ToString());
    }

    private static Stream StringToStream(string src)
    {
        var byteArray = Encoding.UTF8.GetBytes(src);
        return new MemoryStream(byteArray);
    }

    private static Stream ObjectToStream(object obj)
    {
        return StringToStream(JsonConvert.SerializeObject(obj));
    }

    public HttpContext Build()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = _httpMethod,
                Path = new PathString(_path),
                Body = StringToStream(_body)
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };

        foreach (var header in _headers)
        {
            context.Request.Headers.Add(header.Key, new StringValues(header.Value));
        }

        return context;
    }

    public HttpContextBuilder WithBody(object message)
    {
        _body = JsonConvert.SerializeObject(message);
        return this;
    }

    public HttpContextBuilder WithRawBody(string body)
    {
        _body = body;
        return this;
    }

    public HttpContextBuilder WithBodyFromFile(string path)
    {
        _body = File.ReadAllText(path);
        return this;
    }

    public HttpContextBuilder WithBodyFromFile(string path, IDictionary<string, string> parameters)
    {
        _body = File.ReadAllText(path);

        foreach (var parameter in parameters)
        {
            _body = _body.Replace("{{" + parameter.Key + "}}", parameter.Value);
        }

        return this;
    }

    public HttpContextBuilder WithPathParameter(string key, string value)
    {
        _pathParameters.Add(key, value);
        return this;
    }

    public HttpContextBuilder WithHeader(string key, string value)
    {
        _headers.Add(key, value);
        return this;
    }

    public HttpContextBuilder WithBearerToken(string token)
    {
        _headers.Add("Authorization", $"Bearer {token}");
        return this;
    }
}
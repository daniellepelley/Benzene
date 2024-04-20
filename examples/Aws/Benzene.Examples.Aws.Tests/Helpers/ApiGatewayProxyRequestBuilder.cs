using System;
using System.Collections.Generic;
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws.Tests.Helpers;

public class ApiGatewayProxyRequestBuilder
{
    private readonly IDictionary<string, string> _headers = new Dictionary<string, string>();
    private readonly string _httpMethod;
    private readonly string _path;
    private readonly IDictionary<string, string> _pathParameters = new Dictionary<string, string>();
    private string _body = "";

    public ApiGatewayProxyRequestBuilder(string httpMethod, string path)
    {
        _path = path;
        _httpMethod = httpMethod;
        _headers.Add("x-correlation-id", Guid.NewGuid().ToString());
    }

    public APIGatewayProxyRequest Build()
    {
        return new APIGatewayProxyRequest
        {
            HttpMethod = _httpMethod,
            Path = _path,
            Body = _body,
            Headers = _headers,
            PathParameters = _pathParameters
        };
    }

    public ApiGatewayProxyRequestBuilder WithBody(object message)
    {
        _body = JsonConvert.SerializeObject(message);
        return this;
    }

    public ApiGatewayProxyRequestBuilder WithXmlBody<T>(T message)
    {
        _body = XmlHelper.ToXml(message);
        return this;
    }

    public ApiGatewayProxyRequestBuilder WithRawBody(string body)
    {
        _body = body;
        return this;
    }

    public ApiGatewayProxyRequestBuilder WithBodyFromFile(string path)
    {
        _body = File.ReadAllText(path);
        return this;
    }

    public ApiGatewayProxyRequestBuilder WithBodyFromFile(string path, IDictionary<string, string> parameters)
    {
        _body = File.ReadAllText(path);

        foreach (var parameter in parameters)
        {
            _body = _body.Replace("{{" + parameter.Key + "}}", parameter.Value);
        }

        return this;
    }

    public ApiGatewayProxyRequestBuilder WithPathParameter(string key, string value)
    {
        _pathParameters.Add(key, value);
        return this;
    }

    public ApiGatewayProxyRequestBuilder WithHeader(string key, string value)
    {
        _headers.Add(key, value);
        return this;
    }

    public ApiGatewayProxyRequestBuilder WithBearerToken(string token)
    {
        _headers.Add("Authorization", $"Bearer {token}");
        return this;
    }
}
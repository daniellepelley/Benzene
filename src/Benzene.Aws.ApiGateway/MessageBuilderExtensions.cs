using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Benzene.Abstractions;
using Benzene.Aws.Core;

namespace Benzene.Aws.ApiGateway;

public static class MessageBuilderExtensions
{
    public static APIGatewayProxyRequest AsApiGatewayRequest(this IHttpBuilder source)
    {
        return new APIGatewayProxyRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Body = System.Text.Json.JsonSerializer.Serialize(source.Message),
            Headers = source.Headers
        };
    }

    public static APIGatewayCustomAuthorizerRequest AsApiGatewayCustomAuthorizerEvent(this IHttpBuilder source)
    {
        return new APIGatewayCustomAuthorizerRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Headers = new Dictionary<string, string>
            {
                { "x-correlation-id", Guid.NewGuid().ToString() }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ApiId = "some-id"
            }
        };
    }
}
using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Serialization;

namespace Benzene.Aws.ApiGateway;

public static class MessageBuilderExtensions
{
    public static APIGatewayProxyRequest AsApiGatewayRequest<T>(this IHttpBuilder<T> source)
        where T : class
        => AsApiGatewayRequest(source, new JsonSerializer());
    
    public static APIGatewayProxyRequest AsApiGatewayRequest<T>(this IHttpBuilder<T> source, ISerializer serializer)
        where T : class
    {
        return new APIGatewayProxyRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Body = serializer.Serialize(source.Message),
            Headers = source.Headers
        };
    }
    public static APIGatewayCustomAuthorizerRequest AsApiGatewayCustomAuthorizerEvent<T>(this IHttpBuilder<T> source)
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
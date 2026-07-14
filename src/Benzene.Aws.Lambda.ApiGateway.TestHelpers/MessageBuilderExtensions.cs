using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Lambda.ApiGateway.TestHelpers;

/// <summary>
/// Provides extension methods for building API Gateway test events from <see cref="IHttpBuilder{T}"/>.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds an <see cref="APIGatewayProxyRequest"/> from an HTTP builder, serializing the message
    /// body with the default JSON serializer.
    /// </summary>
    /// <typeparam name="T">The type of the message body.</typeparam>
    /// <param name="source">The HTTP builder to convert.</param>
    /// <returns>The built API Gateway proxy request.</returns>
    public static APIGatewayProxyRequest AsApiGatewayRequest<T>(this IHttpBuilder<T> source)
        where T : class
        => AsApiGatewayRequest(source, new JsonSerializer());

    /// <summary>
    /// Builds an <see cref="APIGatewayProxyRequest"/> from an HTTP builder, serializing the message
    /// body with the given serializer.
    /// </summary>
    /// <typeparam name="T">The type of the message body.</typeparam>
    /// <param name="source">The HTTP builder to convert.</param>
    /// <param name="serializer">The serializer used to serialize the message body.</param>
    /// <returns>The built API Gateway proxy request.</returns>
    public static APIGatewayProxyRequest AsApiGatewayRequest<T>(this IHttpBuilder<T> source, ISerializer serializer)
        where T : class
    {
        return new APIGatewayProxyRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Body = source.Message != null ? serializer.Serialize(source.Message) : null,
            Headers = source.Headers
        };
    }

    /// <summary>
    /// Builds an <see cref="APIGatewayCustomAuthorizerRequest"/> from an HTTP builder.
    /// </summary>
    /// <typeparam name="T">The type of the message body.</typeparam>
    /// <param name="source">The HTTP builder to convert.</param>
    /// <param name="apiId">The API Gateway API ID to use in the request context.</param>
    /// <returns>The built API Gateway custom authorizer request.</returns>
    public static APIGatewayCustomAuthorizerRequest AsApiGatewayCustomAuthorizerEvent<T>(this IHttpBuilder<T> source, string apiId = "some-id")
    {
        return new APIGatewayCustomAuthorizerRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Headers = source.Headers,
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ApiId = apiId
            }
        };
    }
}

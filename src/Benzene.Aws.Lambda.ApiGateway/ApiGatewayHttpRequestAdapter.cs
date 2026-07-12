using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Adapts an <see cref="ApiGatewayContext"/> into Benzene's transport-agnostic <see cref="HttpRequest"/> shape.
/// </summary>
public class ApiGatewayHttpRequestAdapter : IHttpRequestAdapter<ApiGatewayContext>
{
    /// <summary>
    /// Maps the API Gateway request onto a Benzene <see cref="HttpRequest"/>.
    /// </summary>
    /// <param name="context">The API Gateway context to map.</param>
    /// <returns>The mapped HTTP request.</returns>
    public HttpRequest Map(ApiGatewayContext context)
    {
        return new HttpRequest
        {
            Path = context.ApiGatewayProxyRequest.Path,
            Method = context.ApiGatewayProxyRequest.HttpMethod,
            Headers = context.ApiGatewayProxyRequest.Headers
        };
    }
}

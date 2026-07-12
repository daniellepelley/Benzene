using Amazon.Lambda.APIGatewayEvents;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Provides the middleware pipeline context for an API Gateway request, wrapping the raw
/// AWS API Gateway proxy request and response.
/// </summary>
public class ApiGatewayContext : IHttpContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayContext"/> class.
    /// </summary>
    /// <param name="apiGatewayProxyRequest">The raw API Gateway proxy request.</param>
    public ApiGatewayContext(APIGatewayProxyRequest apiGatewayProxyRequest)
    {
        ApiGatewayProxyRequest = apiGatewayProxyRequest;
    }

    /// <summary>
    /// Gets the raw API Gateway proxy request.
    /// </summary>
    public APIGatewayProxyRequest ApiGatewayProxyRequest { get; }

    /// <summary>
    /// Gets or sets the API Gateway proxy response to return. Populated by response middleware as the
    /// pipeline executes; use <see cref="Extensions.EnsureResponseExists"/> to lazily initialize it.
    /// </summary>
    public APIGatewayProxyResponse ApiGatewayProxyResponse { get; set; }
}

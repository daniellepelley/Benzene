using Amazon.Lambda.APIGatewayEvents;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// Provides the middleware pipeline context for an API Gateway custom authorizer request, wrapping the
/// raw AWS custom authorizer request and response.
/// </summary>
public class ApiGatewayCustomAuthorizerContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayCustomAuthorizerContext"/> class.
    /// </summary>
    /// <param name="apiGatewayCustomAuthorizerRequest">The raw API Gateway custom authorizer request.</param>
    public ApiGatewayCustomAuthorizerContext(APIGatewayCustomAuthorizerRequest apiGatewayCustomAuthorizerRequest)
    {
        ApiGatewayCustomAuthorizerRequest = apiGatewayCustomAuthorizerRequest;
    }

    /// <summary>
    /// Gets the raw API Gateway custom authorizer request.
    /// </summary>
    public APIGatewayCustomAuthorizerRequest ApiGatewayCustomAuthorizerRequest { get; }

    /// <summary>
    /// Gets or sets the custom authorizer response (typically an IAM policy document) to return.
    /// </summary>
    public APIGatewayCustomAuthorizerResponse ApiGatewayCustomAuthorizerResponse { get; set; }
}

using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public class ApiGatewayCustomAuthorizerContext 
{
    public ApiGatewayCustomAuthorizerContext(APIGatewayCustomAuthorizerRequest apiGatewayCustomAuthorizerRequest)
    {
        ApiGatewayCustomAuthorizerRequest = apiGatewayCustomAuthorizerRequest;
    }

    public APIGatewayCustomAuthorizerRequest ApiGatewayCustomAuthorizerRequest { get; }
    public APIGatewayCustomAuthorizerResponse ApiGatewayCustomAuthorizerResponse { get; set; }
}

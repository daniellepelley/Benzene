using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public class ApiGatewayCustomAuthorizerContext : IHasMessageResult
{
    public ApiGatewayCustomAuthorizerContext(APIGatewayCustomAuthorizerRequest apiGatewayCustomAuthorizerRequest)
    {
        ApiGatewayCustomAuthorizerRequest = apiGatewayCustomAuthorizerRequest;
        MessageResult = Benzene.Core.Results.MessageResult.Empty();
    }

    public APIGatewayCustomAuthorizerRequest ApiGatewayCustomAuthorizerRequest { get; }
    public APIGatewayCustomAuthorizerResponse ApiGatewayCustomAuthorizerResponse { get; set; }
    public IMessageResult MessageResult { get; set; }
}

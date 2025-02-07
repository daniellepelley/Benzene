using Amazon.Lambda.APIGatewayEvents;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

public class ApiGatewayContext : IHttpContext
{
    public ApiGatewayContext(APIGatewayProxyRequest apiGatewayProxyRequest)
    {
        ApiGatewayProxyRequest = apiGatewayProxyRequest;
    }

    public APIGatewayProxyRequest ApiGatewayProxyRequest { get; }
    public APIGatewayProxyResponse ApiGatewayProxyResponse { get; set; }
}
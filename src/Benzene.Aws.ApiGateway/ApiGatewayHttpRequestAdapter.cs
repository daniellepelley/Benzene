using Benzene.Http;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayHttpRequestAdapter : IHttpRequestAdapter<ApiGatewayContext>
{
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
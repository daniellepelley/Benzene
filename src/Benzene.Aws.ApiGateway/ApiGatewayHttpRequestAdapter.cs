namespace Benzene.Aws.ApiGateway;

public class ApiGatewayHttpRequestAdapter : IHttpRequestAdapter<ApiGatewayContext>
{
    public HttpRequest2 Map(ApiGatewayContext context)
    {
        return new HttpRequest2
        {
            Path = context.ApiGatewayProxyRequest.Path,
            Method = context.ApiGatewayProxyRequest.HttpMethod,
            Headers = context.ApiGatewayProxyRequest.Headers
        };
    }
}
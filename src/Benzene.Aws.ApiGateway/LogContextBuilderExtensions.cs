using Benzene.Core.Logging;

namespace Benzene.Aws.ApiGateway;

public static class LogContextBuilderExtensions
{
    public static LogContextBuilder<ApiGatewayContext> WithHttp(this LogContextBuilder<ApiGatewayContext> source)
    {
        return source
            .OnRequest("path", (_, context) => context.ApiGatewayProxyRequest.Path)
            .OnRequest("method", (_, context) => context.ApiGatewayProxyRequest.HttpMethod);
    }
}

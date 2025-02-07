using Benzene.Abstractions.Logging;
using Benzene.Core.Logging;

namespace Benzene.Aws.Lambda.ApiGateway;

public static class LogContextBuilderExtensions
{
    public static ILogContextBuilder<ApiGatewayContext> WithHttp(this LogContextBuilder<ApiGatewayContext> source)
    {
        return source
            .OnRequest("path", (_, context) => context.ApiGatewayProxyRequest.Path)
            .OnRequest("method", (_, context) => context.ApiGatewayProxyRequest.HttpMethod);
    }
}

using Benzene.Core.Logging;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public static class LogContextBuilderExtensions
{
    public static LogContextBuilder<ApiGatewayCustomAuthorizerContext> WithHttp(this LogContextBuilder<ApiGatewayCustomAuthorizerContext> source)
    {
        return source
            .OnRequest("path", (_, context) => context.ApiGatewayCustomAuthorizerRequest.Path)
            .OnRequest("method", (_, context) => context.ApiGatewayCustomAuthorizerRequest.HttpMethod);
    }
}

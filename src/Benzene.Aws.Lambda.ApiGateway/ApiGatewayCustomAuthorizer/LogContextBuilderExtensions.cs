using Benzene.Abstractions.Logging;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// Provides extension methods for adding custom authorizer-specific fields to a log context builder.
/// </summary>
public static class LogContextBuilderExtensions
{
    /// <summary>
    /// Adds the request path and HTTP method to the log context.
    /// </summary>
    /// <param name="source">The log context builder to extend.</param>
    /// <returns>The log context builder for method chaining.</returns>
    public static ILogContextBuilder<ApiGatewayCustomAuthorizerContext> WithHttp(this ILogContextBuilder<ApiGatewayCustomAuthorizerContext> source)
    {
        return source
            .OnRequest("path", (_, context) => context.ApiGatewayCustomAuthorizerRequest.Path)
            .OnRequest("method", (_, context) => context.ApiGatewayCustomAuthorizerRequest.HttpMethod);
    }
}

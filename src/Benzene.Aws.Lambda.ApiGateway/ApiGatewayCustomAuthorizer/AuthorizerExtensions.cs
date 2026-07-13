using System;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// Provides a first-class way to implement an API Gateway custom (Lambda) authorizer as middleware:
/// inspect the incoming request and return the authorizer response (an IAM policy document).
/// </summary>
public static class AuthorizerExtensions
{
    /// <summary>
    /// Adds a custom authorizer step that produces the <see cref="APIGatewayCustomAuthorizerResponse"/>
    /// for the request, with access to the current <see cref="IServiceResolver"/> for dependency injection
    /// (e.g. to resolve a token validator).
    /// </summary>
    /// <param name="app">The custom authorizer pipeline builder.</param>
    /// <param name="authorize">A delegate that inspects the request and returns the authorizer response.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext> UseCustomAuthorizer(
        this IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext> app,
        Func<APIGatewayCustomAuthorizerRequest, IServiceResolver, Task<APIGatewayCustomAuthorizerResponse>> authorize)
    {
        return app.Use("CustomAuthorizer",
            (IServiceResolver resolver, ApiGatewayCustomAuthorizerContext context, Func<Task> next)
                => AuthorizeAsync(resolver, context, next, authorize));
    }

    /// <summary>
    /// Adds a custom authorizer step that produces the <see cref="APIGatewayCustomAuthorizerResponse"/>
    /// for the request.
    /// </summary>
    /// <param name="app">The custom authorizer pipeline builder.</param>
    /// <param name="authorize">A delegate that inspects the request and returns the authorizer response.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext> UseCustomAuthorizer(
        this IMiddlewarePipelineBuilder<ApiGatewayCustomAuthorizerContext> app,
        Func<APIGatewayCustomAuthorizerRequest, Task<APIGatewayCustomAuthorizerResponse>> authorize)
    {
        return app.UseCustomAuthorizer((request, _) => authorize(request));
    }

    private static async Task AuthorizeAsync(
        IServiceResolver resolver,
        ApiGatewayCustomAuthorizerContext context,
        Func<Task> next,
        Func<APIGatewayCustomAuthorizerRequest, IServiceResolver, Task<APIGatewayCustomAuthorizerResponse>> authorize)
    {
        context.ApiGatewayCustomAuthorizerResponse =
            await authorize(context.ApiGatewayCustomAuthorizerRequest, resolver);
        await next();
    }
}

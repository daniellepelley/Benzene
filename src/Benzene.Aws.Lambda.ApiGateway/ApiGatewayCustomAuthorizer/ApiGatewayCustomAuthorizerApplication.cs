using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// Wraps the custom authorizer middleware pipeline as a <see cref="MiddlewareApplication{TEvent,TContext,TResult}"/>,
/// converting an <see cref="APIGatewayCustomAuthorizerRequest"/> into an
/// <see cref="ApiGatewayCustomAuthorizerContext"/> and back into an <see cref="APIGatewayCustomAuthorizerResponse"/>.
/// </summary>
public class ApiGatewayCustomAuthorizerApplication : MiddlewareApplication<APIGatewayCustomAuthorizerRequest, ApiGatewayCustomAuthorizerContext, APIGatewayCustomAuthorizerResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayCustomAuthorizerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built custom authorizer middleware pipeline.</param>
    public ApiGatewayCustomAuthorizerApplication(IMiddlewarePipeline<ApiGatewayCustomAuthorizerContext> pipeline)
        : base(new TransportMiddlewarePipeline<ApiGatewayCustomAuthorizerContext>(TransportNames.ApiGateway, pipeline),
            @event => new ApiGatewayCustomAuthorizerContext(@event),
            context => context.ApiGatewayCustomAuthorizerResponse
        )
    { }
}

using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Wraps the API Gateway middleware pipeline as a <see cref="MiddlewareApplication{TEvent,TContext,TResult}"/>,
/// converting an <see cref="APIGatewayProxyRequest"/> into an <see cref="ApiGatewayContext"/> and back
/// into an <see cref="APIGatewayProxyResponse"/>.
/// </summary>
public class ApiGatewayApplication : MiddlewareApplication<APIGatewayProxyRequest, ApiGatewayContext, APIGatewayProxyResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built API Gateway middleware pipeline.</param>
    public ApiGatewayApplication(IMiddlewarePipeline<ApiGatewayContext> pipeline)
        : base(new TransportMiddlewarePipeline<ApiGatewayContext>(TransportNames.ApiGateway, pipeline),
            @event => new ApiGatewayContext(@event),
            context => context.ApiGatewayProxyResponse)
    { }
}

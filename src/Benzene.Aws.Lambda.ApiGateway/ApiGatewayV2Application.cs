using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Wraps the API Gateway HTTP API v2 middleware pipeline as a
/// <see cref="MiddlewareApplication{TEvent,TContext,TResult}"/>, converting an
/// <see cref="APIGatewayHttpApiV2ProxyRequest"/> into an <see cref="ApiGatewayV2Context"/> and back
/// into an <see cref="APIGatewayHttpApiV2ProxyResponse"/>.
/// </summary>
public class ApiGatewayV2Application : MiddlewareApplication<APIGatewayHttpApiV2ProxyRequest, ApiGatewayV2Context, APIGatewayHttpApiV2ProxyResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2Application"/> class.
    /// </summary>
    /// <param name="pipeline">The built API Gateway v2 middleware pipeline.</param>
    public ApiGatewayV2Application(IMiddlewarePipeline<ApiGatewayV2Context> pipeline)
        : base(new TransportMiddlewarePipeline<ApiGatewayV2Context>(TransportNames.ApiGateway, pipeline),
            @event => new ApiGatewayV2Context(@event),
            context => context.ApiGatewayProxyResponse)
    { }
}

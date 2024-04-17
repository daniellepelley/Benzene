using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayApplication : MiddlewareApplication<APIGatewayProxyRequest, ApiGatewayContext, APIGatewayProxyResponse>
{
    public ApiGatewayApplication(IMiddlewarePipeline<ApiGatewayContext> pipeline)
        : base(pipeline,
            @event => new ApiGatewayContext(@event),
            context => context.ApiGatewayProxyResponse)
    { }
}

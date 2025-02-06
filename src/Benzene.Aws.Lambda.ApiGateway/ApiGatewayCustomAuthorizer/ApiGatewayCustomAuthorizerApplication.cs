using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public class ApiGatewayCustomAuthorizerApplication : MiddlewareApplication<APIGatewayCustomAuthorizerRequest, ApiGatewayCustomAuthorizerContext, APIGatewayCustomAuthorizerResponse>
{
    public ApiGatewayCustomAuthorizerApplication(IMiddlewarePipeline<ApiGatewayCustomAuthorizerContext> pipeline)
        : base(pipeline,
            @event => new ApiGatewayCustomAuthorizerContext(@event),
            context => context.ApiGatewayCustomAuthorizerResponse
        )
    { }
}

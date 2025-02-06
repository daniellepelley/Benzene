using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public class ApiGatewayCustomAuthorizerLambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayCustomAuthorizerRequest>
{
    private readonly ApiGatewayCustomAuthorizerApplication _apiGatewayApplication;

    public ApiGatewayCustomAuthorizerLambdaHandler(IMiddlewarePipeline<ApiGatewayCustomAuthorizerContext> pipeline, IServiceResolver serviceResolver)
        :base(serviceResolver)
    {
        _apiGatewayApplication = new ApiGatewayCustomAuthorizerApplication(pipeline);
    }

    protected override bool CanHandle(APIGatewayCustomAuthorizerRequest request)
    {
        return !string.IsNullOrEmpty(request?.RequestContext?.ApiId);
    }

    protected override async Task HandleFunction(APIGatewayCustomAuthorizerRequest request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _apiGatewayApplication.HandleAsync(request, serviceResolverFactory);
        JsonSerializer.Serialize(response, context.Response);
        if (context.Response != null)
        {
            context.Response.Position = 0;
        }
    }
}

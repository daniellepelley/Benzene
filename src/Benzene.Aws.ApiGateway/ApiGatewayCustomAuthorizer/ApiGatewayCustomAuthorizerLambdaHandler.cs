using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Results;

namespace Benzene.Aws.ApiGateway.ApiGatewayCustomAuthorizer;

public class ApiGatewayCustomAuthorizerLambdaHandler : AwsLambdaHandlerMiddleware<APIGatewayCustomAuthorizerRequest>
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

    protected override async Task HandleFunction(APIGatewayCustomAuthorizerRequest request, AwsEventStreamContext context, IServiceResolver serviceResolver)
    {
        var response = await _apiGatewayApplication.HandleAsync(request, serviceResolver);
        JsonSerializer.Serialize(response, context.Response);
        if (context.Response != null)
        {
            context.Response.Position = 0;
        }
    }
}

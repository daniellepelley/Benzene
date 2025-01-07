using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayLambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayProxyRequest>
{
    private readonly ApiGatewayApplication _apiGatewayApplication;

    public ApiGatewayLambdaHandler(IMiddlewarePipeline<ApiGatewayContext> pipeline,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _apiGatewayApplication = new ApiGatewayApplication(pipeline);
    }

    protected override bool CanHandle(APIGatewayProxyRequest request)
    {
        return request?.HttpMethod != null;
    }

    protected override async Task HandleFunction(APIGatewayProxyRequest request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _apiGatewayApplication.HandleAsync(request, serviceResolverFactory);

        MapResponse(context, response);
    }

}

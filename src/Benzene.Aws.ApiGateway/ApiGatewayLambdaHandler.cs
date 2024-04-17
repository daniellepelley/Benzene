using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayLambdaHandler : AwsLambdaHandlerMiddleware<APIGatewayProxyRequest>
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

    protected override async Task HandleFunction(APIGatewayProxyRequest request, AwsEventStreamContext context, IServiceResolver serviceResolver)
    {
        var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        setCurrentTransport.SetTransport("api-gateway");
        var response = await _apiGatewayApplication.HandleAsync(request, serviceResolver);

        MapResponse(context, response);
    }

}

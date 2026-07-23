using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into an <see cref="APIGatewayProxyRequest"/>
/// to the API Gateway middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by
/// <see cref="Extensions.UseApiGateway"/>. It only handles the invocation if the payload has an
/// HTTP method; otherwise it defers to the next middleware.
/// </remarks>
public class ApiGatewayLambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayProxyRequest>
{
    // Source-generated JSON metadata for the API Gateway proxy event types, built once per process
    // (shared, thread-safe). Replaces the base router's reflection-based DefaultLambdaJsonSerializer for
    // this handler, so the first invocation doesn't pay System.Text.Json's per-type metadata build - the
    // bulk of the cold-start API-Gateway-to-Benzene conversion cost in the X-Ray cold-start analysis.
    private static readonly SourceGeneratorLambdaJsonSerializer<ApiGatewayJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly ApiGatewayApplication _apiGatewayApplication;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayLambdaHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The built API Gateway middleware pipeline to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public ApiGatewayLambdaHandler(IMiddlewarePipeline<ApiGatewayContext> pipeline,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _apiGatewayApplication = new ApiGatewayApplication(pipeline);
    }

    /// <summary>
    /// Determines whether the deserialized request looks like an API Gateway request.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the request has a non-null HTTP method; otherwise, false.</returns>
    protected override bool CanHandle(APIGatewayProxyRequest request)
    {
        return request?.HttpMethod != null;
    }

    /// <summary>
    /// Handles the request by running it through the API Gateway pipeline and writing the response.
    /// </summary>
    /// <param name="request">The API Gateway request extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(APIGatewayProxyRequest request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _apiGatewayApplication.HandleAsync(request, serviceResolverFactory);

        MapResponse(context, response);
    }

}

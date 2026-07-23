using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into an
/// <see cref="APIGatewayHttpApiV2ProxyRequest"/> (API Gateway HTTP API, payload format version 2.0)
/// to the API Gateway v2 middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by
/// <see cref="Extensions.UseApiGatewayV2"/>. It only handles the invocation if the payload carries a
/// v2 discriminant (<c>version == "2.0"</c> or a <c>RequestContext.Http.Method</c>); otherwise it
/// defers to the next middleware. Because a v1 event has neither of those fields (its method lives at
/// the top level as <c>httpMethod</c>), this router is mutually exclusive with
/// <see cref="ApiGatewayLambdaHandler"/> — an app can register both and each claims only its own
/// payload shape, regardless of registration order.
/// </remarks>
public class ApiGatewayV2LambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayHttpApiV2ProxyRequest>
{
    // Source-generated JSON metadata for the API Gateway v2 event types, built once per process,
    // replacing the base router's reflection serializer so the first (cold) invocation skips the
    // metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<ApiGatewayV2JsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly ApiGatewayV2Application _apiGatewayApplication;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2LambdaHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The built API Gateway v2 middleware pipeline to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public ApiGatewayV2LambdaHandler(IMiddlewarePipeline<ApiGatewayV2Context> pipeline,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _apiGatewayApplication = new ApiGatewayV2Application(pipeline);
    }

    /// <summary>
    /// Determines whether the deserialized request looks like an API Gateway HTTP API v2 request.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>
    /// True if the request declares payload format <c>2.0</c> or carries a
    /// <c>RequestContext.Http.Method</c>; otherwise, false.
    /// </returns>
    protected override bool CanHandle(APIGatewayHttpApiV2ProxyRequest request)
    {
        return request?.Version == "2.0" || request?.RequestContext?.Http?.Method != null;
    }

    /// <summary>
    /// Handles the request by running it through the API Gateway v2 pipeline and writing the response.
    /// </summary>
    /// <param name="request">The API Gateway v2 request extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(APIGatewayHttpApiV2ProxyRequest request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _apiGatewayApplication.HandleAsync(request, serviceResolverFactory);

        MapResponse(context, response);
    }
}

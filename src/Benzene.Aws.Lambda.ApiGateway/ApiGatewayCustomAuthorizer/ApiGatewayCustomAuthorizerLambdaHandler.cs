using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.ApiGateway.ApiGatewayCustomAuthorizer;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into an <see cref="APIGatewayCustomAuthorizerRequest"/>
/// to the custom authorizer middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by
/// <see cref="Extensions.UseApiGatewayCustomAuthorizer"/>. It only handles the invocation if the payload
/// has a non-empty API ID; otherwise it defers to the next middleware.
/// </remarks>
public class ApiGatewayCustomAuthorizerLambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayCustomAuthorizerRequest>
{
    // Source-generated JSON metadata for the custom authorizer event types, built once per process,
    // replacing the base router's reflection serializer so the first (cold) invocation skips the
    // metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<ApiGatewayCustomAuthorizerJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly ApiGatewayCustomAuthorizerApplication _apiGatewayApplication;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayCustomAuthorizerLambdaHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The built custom authorizer middleware pipeline to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public ApiGatewayCustomAuthorizerLambdaHandler(IMiddlewarePipeline<ApiGatewayCustomAuthorizerContext> pipeline, IServiceResolver serviceResolver)
        :base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _apiGatewayApplication = new ApiGatewayCustomAuthorizerApplication(pipeline);
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a custom authorizer request.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the request has a non-empty API ID; otherwise, false.</returns>
    protected override bool CanHandle(APIGatewayCustomAuthorizerRequest request)
    {
        return !string.IsNullOrEmpty(request?.RequestContext?.ApiId);
    }

    /// <summary>
    /// Handles the request by running it through the custom authorizer pipeline and writing the response.
    /// </summary>
    /// <param name="request">The custom authorizer request extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
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

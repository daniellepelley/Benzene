using System.Threading.Tasks;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Aws.Lambda.Core.BenzeneMessage;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into a <see cref="BenzeneMessageRequest"/>
/// (i.e. a topic-and-body message) to the transport-agnostic BenzeneMessage pipeline.
/// </summary>
/// <remarks>
/// This middleware is added to the outer <see cref="AwsEventStreamContext"/> pipeline by
/// <see cref="Extensions.UseBenzeneMessage(Benzene.Abstractions.Middleware.IMiddlewarePipelineBuilder{AwsEventStreamContext}, System.Action{Benzene.Abstractions.Middleware.IMiddlewarePipelineBuilder{Benzene.Core.Messages.BenzeneMessage.BenzeneMessageContext}})"/>.
/// It only handles the invocation if the payload has a <see cref="BenzeneMessageRequest.Topic"/>; otherwise
/// it defers to the next middleware, allowing other event source adapters (API Gateway, SQS, ...) to attempt
/// to handle the same raw payload.
/// </remarks>
public class BenzeneMessageLambdaHandler : AwsLambdaMiddlewareRouter<BenzeneMessageRequest>
{
    // Source-generated JSON metadata for the BenzeneMessage request/response types, built once per
    // process, replacing the base router's reflection serializer so the first (cold) invocation skips
    // the metadata build. This is the Lambda-to-Lambda direct-invoke path the mesh uses.
    private static readonly SourceGeneratorLambdaJsonSerializer<BenzeneMessageJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly BenzeneMessageApplication _directMessageApplication;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageLambdaHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The built BenzeneMessage middleware pipeline to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public BenzeneMessageLambdaHandler(
        IMiddlewarePipeline<BenzeneMessageContext> pipeline,
        IServiceResolver serviceResolver)
    :base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _directMessageApplication = new BenzeneMessageApplication(pipeline);
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a BenzeneMessage request.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the request has a non-null topic; otherwise, false.</returns>
    protected override bool CanHandle(BenzeneMessageRequest request)
    {
        return request?.Topic != null;
    }

    /// <summary>
    /// Handles the request by running it through the BenzeneMessage pipeline and writing the response.
    /// </summary>
    /// <param name="request">The BenzeneMessage request extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolver">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(BenzeneMessageRequest request, AwsEventStreamContext context, IServiceResolverFactory serviceResolver)
    {
        // var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        // setCurrentTransport.SetTransport("direct");
        var response = await _directMessageApplication.HandleAsync(request, serviceResolver);
        MapResponse(context, response);
    }
}

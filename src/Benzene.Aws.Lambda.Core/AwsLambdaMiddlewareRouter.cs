using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides a base class for AWS Lambda middleware that attempts to deserialize the raw
/// <see cref="AwsEventStreamContext"/> payload into a specific event source request type.
/// </summary>
/// <typeparam name="TRequest">The event source request type this middleware handles (e.g. an SQS or SNS event).</typeparam>
/// <remarks>
/// Implementations of the various AWS event source adapters (API Gateway, SQS, SNS, EventBridge, ...)
/// derive from this class. Each one attempts to deserialize the stream into its own <typeparamref name="TRequest"/>
/// type and, if that succeeds and the request looks like its event type, handles it and writes a response.
/// </remarks>
public abstract class AwsLambdaMiddlewareRouter<TRequest> : MiddlewareRouter<TRequest, AwsEventStreamContext>
{
    // Shared across every instance of a closed router type: the pipeline resolves middleware fresh
    // per invocation, and System.Text.Json caches its reflection-built type metadata per
    // JsonSerializerOptions instance - so a per-instance serializer re-paid the full metadata build
    // for TRequest (tens of milliseconds for the large AWS event types) on every single invocation.
    private static readonly DefaultLambdaJsonSerializer SharedJsonSerializer = new();

    /// <summary>
    /// The JSON serializer used to deserialize the request and serialize the response. Defaults to the
    /// reflection-based <see cref="DefaultLambdaJsonSerializer"/>, shared across router instances (it's
    /// thread-safe and stateless per call). Typed as <see cref="ILambdaSerializer"/> so a derived router
    /// can assign a <see cref="SourceGeneratorLambdaJsonSerializer{TContext}"/> for its event type -
    /// removing System.Text.Json's per-type metadata build from the first (cold) invocation.
    /// </summary>
    protected ILambdaSerializer JsonSerializer = SharedJsonSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaMiddlewareRouter{TRequest}"/> class.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    protected AwsLambdaMiddlewareRouter(IServiceResolver serviceResolver)
        :base(serviceResolver)
    {
    }

    /// <summary>
    /// Attempts to deserialize the context's stream into <typeparamref name="TRequest"/>.
    /// </summary>
    /// <param name="context">The AWS event stream context to read the request from.</param>
    /// <returns>The deserialized request, or the default value of <typeparamref name="TRequest"/> if deserialization fails.</returns>
    protected override TRequest TryExtractRequest(AwsEventStreamContext context)
    {
        context.Stream.Position = 0;

        try
        {
            return JsonSerializer.Deserialize<TRequest>(context.Stream);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Serializes a response and writes it to the context's response stream.
    /// </summary>
    /// <typeparam name="TResponse">The static response type. Passed through to
    /// <see cref="ILambdaSerializer.Serialize{T}"/> so a source-generated serializer can resolve the
    /// right <c>JsonTypeInfo</c> by type (a boxed <c>object</c> would defeat source generation). Every
    /// caller passes the application's concrete response type, so this is also the runtime type - no
    /// behavioural change for the reflection serializer.</typeparam>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="response">The response to serialize.</param>
    protected void MapResponse<TResponse>(AwsEventStreamContext context, TResponse response)
    {
        JsonSerializer.Serialize(response, context.Response);
        if (context.Response != null)
        {
            context.Response.Position = 0;
        }
    }
}

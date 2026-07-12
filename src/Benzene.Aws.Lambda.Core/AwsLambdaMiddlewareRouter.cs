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
    /// <summary>
    /// The JSON serializer used to deserialize the request and serialize the response.
    /// </summary>
    protected DefaultLambdaJsonSerializer JsonSerializer = new();

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
    /// Serializes a response object and writes it to the context's response stream.
    /// </summary>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="response">The response object to serialize.</param>
    protected void MapResponse(AwsEventStreamContext context, object response)
    {
        JsonSerializer.Serialize(response, context.Response);
        if (context.Response != null)
        {
            context.Response.Position = 0;
        }
    }
}

using System.Threading.Tasks;
using Amazon.Lambda.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// Converts between a generic Benzene client context and a <see cref="LambdaSendMessageContext"/>, so
/// that a Benzene client pipeline can invoke messages via AWS Lambda.
/// </summary>
/// <typeparam name="T">The type of the outgoing message.</typeparam>
public class LambdaContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, LambdaSendMessageContext>
{
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaContextConverter{T}"/> class using a
    /// <see cref="JsonSerializer"/> to serialize the outgoing message.
    /// </summary>
    public LambdaContextConverter()
        :this(new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaContextConverter{T}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to serialize the outgoing message.</param>
    public LambdaContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Builds a Lambda invoke request context, serializing the outgoing message as the invocation payload.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context.</param>
    /// <returns>A task that resolves to the built <see cref="LambdaSendMessageContext"/>.</returns>
    /// <remarks>
    /// <see cref="IBenzeneClientRequest{T}.Headers"/> is not forwarded here: a raw <see cref="InvokeRequest"/>
    /// has no header-like concept comparable to HTTP/SQS/SNS/Kafka (AWS's own <c>ClientContext</c> feature
    /// is a distinct, base64-encoded JSON blob with different semantics). A decorator like
    /// <c>WithW3CTraceContext()</c> has no effect on a pipeline built via <c>UseAwsLambda()</c>/this
    /// converter specifically. This does not affect the separate, more commonly used
    /// <see cref="Benzene.Clients.Aws.Lambda.AwsLambdaBenzeneMessageClient"/> (wired via
    /// <c>CreateAwsLambdaBenzeneMessageClient()</c>), which already embeds
    /// <see cref="IBenzeneClientRequest{T}.Headers"/> into the <c>BenzeneMessageClientRequest</c> envelope
    /// it invokes with.
    /// </remarks>
    public Task<LambdaSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        return Task.FromResult(new LambdaSendMessageContext(new InvokeRequest
        {
            Payload = _serializer.Serialize(contextIn.Request.Message)
        }));
    }

    /// <summary>
    /// Marks the incoming Benzene client context as accepted. The Lambda invocation itself does not carry
    /// a mapped response payload through this pipeline shape.
    /// </summary>
    /// <param name="contextIn">The incoming Benzene client context to set the response on.</param>
    /// <param name="contextOut">The completed <see cref="LambdaSendMessageContext"/>.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, LambdaSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}

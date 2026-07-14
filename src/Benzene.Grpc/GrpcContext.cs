using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Request;
using Grpc.Core;

namespace Benzene.Grpc;

public class GrpcContext : IHasMessageResult
{
    public string Topic { get; }
    public ServerCallContext CallContext { get; }

    public virtual object RequestAsObject { get; }
    public virtual object? ResponseAsObject { get; set; }
    public object? ResponsePayload { get; set; }

    /// <summary>The token that's cancelled if the call is cancelled or its deadline is exceeded.</summary>
    public CancellationToken CancellationToken => CallContext.CancellationToken;

    /// <summary>
    /// Metadata to send back to the client before the first response message. Written by the transport
    /// once the pipeline completes; empty means no response headers are sent.
    /// </summary>
    public Metadata ResponseHeaders { get; } = new();

    /// <summary>Metadata to send back to the client after the response. Backed directly by the call.</summary>
    public Metadata ResponseTrailers => CallContext.ResponseTrailers;

    public GrpcContext(string topic, ServerCallContext callContext)
    {
        Topic = topic;
        CallContext = callContext;
    }

    public IMessageResult MessageResult { get; set; }
    public IMessageHandlerResult? MessageHandlerResult { get; set; }
}

public class GrpcContext<TRequest, TResponse> : GrpcContext, IRequestContext<TRequest>
{
    public GrpcContext(string topic, ServerCallContext callContext, TRequest request)
        : base(topic, callContext)
    {
        Request = request;
    }

    public override object RequestAsObject => Request;
    public override object? ResponseAsObject
    {
        get => (object?)Response ?? ResponsePayload;
        set
        {
            if (value is TResponse typed)
            {
                Response = typed;
                return;
            }

            ResponsePayload = value;
        }
    }

    public TRequest Request { get; }
    public TResponse? Response { get; set; }
}

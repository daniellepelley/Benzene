using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Request;

namespace Benzene.Grpc;

public class GrpcContext : IHasMessageResult
{
    public string Topic { get; }

    public virtual object RequestAsObject { get; }
    public virtual object? ResponseAsObject { get; set; }
    public object? ResponsePayload { get; set; }

    public GrpcContext(string topic)
    {
        Topic = topic;
    }

    public IMessageResult MessageResult { get; set; }
    public IMessageHandlerResult? MessageHandlerResult { get; set; }
}

public class GrpcContext<TRequest, TResponse> : GrpcContext, IRequestContext<TRequest>
{
    public GrpcContext(string topic, TRequest request)
        : base(topic)
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

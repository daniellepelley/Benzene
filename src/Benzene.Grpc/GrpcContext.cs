using Benzene.Abstractions.Request;
using Benzene.Abstractions.Results;

namespace Benzene.Grpc;

public class GrpcContext : IHasMessageResult
{
    public string Topic { get; }
   
    public virtual object RequestAsObject { get; }  

    public GrpcContext(string topic)
    {
        Topic = topic;
        MessageResult = Benzene.Core.Results.MessageResult.Empty();
    }

    public IMessageResult MessageResult { get; set; }
}

public class GrpcContext<TRequest> : GrpcContext, IRequestContext<TRequest>
{
    public GrpcContext(string topic, TRequest request)
        : base(topic)
    {
        Request = request;
    }

    public override object RequestAsObject => Request;

    public TRequest Request { get; }
}

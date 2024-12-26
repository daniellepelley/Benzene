using System.Text.Json;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.ToDelete;
using Benzene.Abstractions.Results;

namespace Benzene.Grpc;

public class GrpcContext : IHasMessageResult
{
    public string Topic { get; }
   
    public virtual object RequestAsObject { get; }  
    public virtual object? ResponseAsObject { get; set; }  

    public GrpcContext(string topic)
    {
        Topic = topic;
    }

    public IMessageResult MessageResult { get; set; }
}

public class GrpcContext<TRequest, TResponse> : GrpcContext, IRequestContext<TRequest>
{
    public GrpcContext(string topic, TRequest request)
        : base(topic)
    {
        Request = request;
    }

    public override object RequestAsObject => Request;
    public override object ResponseAsObject
    {
        get { return Response; }
        set
        {
            if (value is TResponse)
            {
                Response = (TResponse)value;
            }

            Response = JsonSerializer.Deserialize<TResponse>(JsonSerializer.Serialize(value));
        }
    }

    public TRequest Request { get; }
    public TResponse? Response { get; set; }
}

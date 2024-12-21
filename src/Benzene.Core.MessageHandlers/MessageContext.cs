using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageContext<TRequest, TResponse> : IMessageContext<TRequest, TResponse>
{
    public MessageContext(ITopic topic, TRequest request)
    {
        Topic = topic;
        Request = request;
        Response = ServiceResult.ServiceUnavailable<TResponse>();
    }

    public ITopic Topic { get; }
    public TRequest Request { get; }
    public IServiceResult<TResponse> Response { get; set; }
}

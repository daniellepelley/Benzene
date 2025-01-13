using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerContext<TRequest, TResponse> : IMessageHandlerContext<TRequest, TResponse>
{
    public MessageHandlerContext(ITopic topic, TRequest request)
    {
        Topic = topic;
        Request = request;
        Response = BenzeneResult.UnexpectedError<TResponse>();
    }

    public ITopic Topic { get; }
    public TRequest Request { get; }
    public IBenzeneResult<TResponse> Response { get; set; }
}

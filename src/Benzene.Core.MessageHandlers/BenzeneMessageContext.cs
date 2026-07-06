using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerContext<TRequest, TResponse> : IMessageHandlerContext<TRequest, TResponse>
{
    public MessageHandlerContext(ITopic topic, TRequest request, Type? handlerType = null)
    {
        Topic = topic;
        Request = request;
        HandlerType = handlerType;
        Response = BenzeneResult.UnexpectedError<TResponse>();
    }

    public ITopic Topic { get; }
    public Type? HandlerType { get; }
    public TRequest Request { get; }
    public IBenzeneResult<TResponse> Response { get; set; }
}

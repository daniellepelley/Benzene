using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerContext<TRequest, TResponse>
{
    ITopic Topic { get; }
    Type? HandlerType { get; }
    TRequest Request { get; }
    IBenzeneResult<TResponse> Response { get; set; }
}
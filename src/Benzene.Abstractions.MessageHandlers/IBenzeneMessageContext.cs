using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerContext<TRequest, TResponse>
{
    ITopic Topic { get; }
    TRequest Request { get; }
    IBenzeneResult<TResponse> Response { get; set; }
}
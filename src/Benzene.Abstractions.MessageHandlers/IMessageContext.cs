using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Abstractions.MessageHandling;

public interface IMessageContext<TRequest, TResponse>
{
    ITopic Topic { get; }
    TRequest Request { get; }
    IServiceResult<TResponse> Response { get; set; }
}
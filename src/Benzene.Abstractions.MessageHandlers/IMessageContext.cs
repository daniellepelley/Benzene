using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageContext<TRequest, TResponse>
{
    ITopic Topic { get; }
    TRequest Request { get; }
    IServiceResult<TResponse> Response { get; set; }
}
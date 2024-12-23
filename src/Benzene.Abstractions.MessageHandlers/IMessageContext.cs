using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageContext<TRequest, TResponse>
{
    ITopic Topic { get; }
    TRequest Request { get; }
    IBenzeneResult<TResponse> Response { get; set; }
}
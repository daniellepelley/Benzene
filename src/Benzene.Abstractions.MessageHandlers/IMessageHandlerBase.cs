using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerBase<TRequest, TResponse>
{
    Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request);
}
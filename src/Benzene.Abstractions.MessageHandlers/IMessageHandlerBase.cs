namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerBase<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request);
}
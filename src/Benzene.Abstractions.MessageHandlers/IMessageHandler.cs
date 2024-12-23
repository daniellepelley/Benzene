using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandler<TRequest, TResponse>
    : IMessageHandlerBase<TRequest, TResponse>
{}

public interface IMessageHandler<TRequest>
{
    Task HandleAsync(TRequest request);
}

public interface IMessageHandler
{
    Task<IBenzeneResult> HandlerAsync(IRequestFactory requestFactory);
}
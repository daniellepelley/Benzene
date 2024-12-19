using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandler<TRequest, TResponse>
    : IMessageHandlerBase<TRequest, IServiceResult<TResponse>>
{}

public interface IMessageHandler<TRequest>
{
    Task HandleAsync(TRequest request);
}

public interface IMessageHandler
{
    Task<IServiceResult> HandlerAsync(IRequestFactory requestFactory);
}
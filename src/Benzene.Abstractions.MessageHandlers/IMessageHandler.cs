using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Abstractions.MessageHandling;

public interface IMessageHandler<TRequest, TResponse>
    : IMessageHandlerBase<TRequest, IServiceResult<TResponse>>
{}

public interface IMessageHandlerBase<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request);
}

public interface IMessageHandler<TRequest>
{
    Task HandleAsync(TRequest request);
}

public interface IMessageHandler
{
    Task<IServiceResult> HandlerAsync(IRequestFactory requestFactory);
}

public interface IPipelineMessageHandler<TRequest, TResponse>
{
    Task<IServiceResult<TResponse>> HandleAsync(ITopic topic, TRequest request);
}


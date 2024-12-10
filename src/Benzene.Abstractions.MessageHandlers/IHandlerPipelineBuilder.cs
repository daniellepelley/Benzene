using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandling;

public interface IHandlerPipelineBuilder
{
    void Add(params IHandlerMiddlewareBuilder[] routerMiddlewareBuilders);

    IMiddlewarePipeline<IMessageContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IMessageHandler<TRequest, TResponse> messageHandler,
        IServiceResolver serviceResolver)
        where TRequest : class;
}

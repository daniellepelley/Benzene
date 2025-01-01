using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandlers;

public interface IHandlerPipelineBuilder
{
    void Add(params IHandlerMiddlewareBuilder[] routerMiddlewareBuilders);

    IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IMessageHandler<TRequest, TResponse> messageHandler,
        IServiceResolver serviceResolver)
        where TRequest : class;
}

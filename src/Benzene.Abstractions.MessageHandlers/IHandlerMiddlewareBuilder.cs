using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandlers;

public interface IHandlerMiddlewareBuilder
{
    IMiddleware<IMessageHandlerContext<TRequest, TResponse>>? Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class;
}

using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Broadcast;

public class BroadcastEventMiddlewareBuilder : IHandlerMiddlewareBuilder
{
    public IMiddleware<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        return new BroadcastEventMiddleware<TRequest, TResponse>(serviceResolver);
    }
}

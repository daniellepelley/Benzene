using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Broadcast;

public class BroadcastEventMiddlewareBuilder : IHandlerMiddlewareBuilder
{
    public IMiddleware<IMessageContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        return new BroadcastEventMiddleware<TRequest, TResponse>(serviceResolver);
    }
}

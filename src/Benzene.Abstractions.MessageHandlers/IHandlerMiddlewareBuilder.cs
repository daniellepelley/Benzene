using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandling;

public interface IHandlerMiddlewareBuilder
{
    IMiddleware<IMessageContext<TRequest, TResponse>>? Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class;
}

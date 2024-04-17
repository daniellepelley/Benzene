using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Abstractions.Middleware;

public interface IHandlerMiddlewareBuilder
{
    IMiddleware<IMessageContext<TRequest, TResponse>>? Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class;
}

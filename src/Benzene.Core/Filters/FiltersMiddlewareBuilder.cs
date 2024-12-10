using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Filters;

public class FiltersMiddlewareBuilder : IHandlerMiddlewareBuilder
{
    public IMiddleware<IMessageContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        return new FiltersMiddleware<TRequest, TResponse>(serviceResolver);
    }
}

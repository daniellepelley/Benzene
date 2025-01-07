using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.FluentValidation;

public class ValidationMiddlewareBuilder : IHandlerMiddlewareBuilder
{
    public IMiddleware<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        return new ValidationMiddleware<TRequest, TResponse>(serviceResolver);
    }
}
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageRouterBuilder : IRegisterDependency
{
    IMessageRouterBuilder Add(IHandlerMiddlewareBuilder handlerMiddlewareBuilder);
    IHandlerMiddlewareBuilder[] GetBuilders();
}
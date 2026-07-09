using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageRouterBuilder : IRegisterDependency
{
    IMessageRouterBuilder Add(IHandlerMiddlewareBuilder handlerMiddlewareBuilder);
    IHandlerMiddlewareBuilder[] GetBuilders();
}
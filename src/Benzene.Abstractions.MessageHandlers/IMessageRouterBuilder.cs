using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.Abstractions.MessageHandling;

public interface IMessageRouterBuilder : IRegisterDependency
{
    IMessageRouterBuilder Add(IHandlerMiddlewareBuilder handlerMiddlewareBuilder);
    IHandlerMiddlewareBuilder[] GetBuilders();
}
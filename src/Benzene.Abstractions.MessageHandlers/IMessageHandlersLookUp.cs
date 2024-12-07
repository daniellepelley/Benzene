using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Abstractions.MessageHandling;

public interface IMessageHandlersLookUp
{
    IMessageHandlerDefinition? FindHandler(ITopic topic);
    IMessageHandlerDefinition[] GetAllHandlers();
}
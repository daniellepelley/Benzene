using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerDefinitionLookUp
{
    IMessageHandlerDefinition? FindHandler(ITopic topic);
    IMessageHandlerDefinition[] GetAllHandlers();
}
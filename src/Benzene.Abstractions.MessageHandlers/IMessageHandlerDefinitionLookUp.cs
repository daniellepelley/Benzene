namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerDefinitionLookUp
{
    IMessageHandlerDefinition? FindHandler(ITopic topic);
    IMessageHandlerDefinition[] GetAllHandlers();
}
namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlersLookUp
{
    IMessageHandlerDefinition? FindHandler(ITopic topic);
    IMessageHandlerDefinition[] GetAllHandlers();
}
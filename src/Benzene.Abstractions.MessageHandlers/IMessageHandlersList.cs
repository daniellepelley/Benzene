namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlersList
{
    void Add(IMessageHandlerDefinition messageHandlerDefinition);
}
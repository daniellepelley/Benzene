namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerFactory
{
    IMessageHandler Create(IMessageHandlerDefinition messageHandlerDefinition);
}
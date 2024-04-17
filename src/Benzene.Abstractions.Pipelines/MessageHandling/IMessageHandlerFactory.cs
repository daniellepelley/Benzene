namespace Benzene.Abstractions.MessageHandling;

public interface IMessageHandlerFactory
{
    IMessageHandler Create(IMessageHandlerDefinition messageHandlerDefinition);
}
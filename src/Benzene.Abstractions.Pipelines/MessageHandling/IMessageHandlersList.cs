namespace Benzene.Abstractions.MessageHandling;

public interface IMessageHandlersList
{
    void Add(IMessageHandlerDefinition messageHandlerDefinition);
}
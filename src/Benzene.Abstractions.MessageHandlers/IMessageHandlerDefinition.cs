namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerDefinition : IRequestResponseMessageDefinition
{
    Type HandlerType { get; }
}
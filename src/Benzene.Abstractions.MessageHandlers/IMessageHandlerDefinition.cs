namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerDefinition : IRequestResponseMessageDefinition
{
    string Version { get; }
    Type HandlerType { get; }
}
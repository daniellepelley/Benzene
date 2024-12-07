namespace Benzene.Abstractions.MessageHandling;

public interface IMessageHandlerDefinition : IRequestResponseMessageDefinition
{
    string Version { get; }
    Type HandlerType { get; }
}
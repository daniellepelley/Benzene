namespace Benzene.Abstractions.MessageHandling;

public interface IMessageSenderDefinition : IRequestResponseMessageDefinition
{
    Type SenderType { get; }
}

namespace Benzene.Abstractions.Messages;

public interface IMessageSenderDefinition : IRequestResponseMessageDefinition
{
    Type SenderType { get; }
}

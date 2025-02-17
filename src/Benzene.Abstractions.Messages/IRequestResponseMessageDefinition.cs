namespace Benzene.Abstractions.Messages;

public interface IRequestResponseMessageDefinition : IMessageDefinition
{
    Type ResponseType { get; }
}
namespace Benzene.Abstractions.MessageHandlers;

public interface IRequestResponseMessageDefinition : IMessageDefinition
{
    Type ResponseType { get; }
}
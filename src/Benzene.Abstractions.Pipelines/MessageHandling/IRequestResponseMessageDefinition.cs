namespace Benzene.Abstractions.MessageHandling;

public interface IRequestResponseMessageDefinition : IMessageDefinition
{
    Type ResponseType { get; }
}
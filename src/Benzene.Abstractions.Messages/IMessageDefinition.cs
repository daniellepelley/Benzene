namespace Benzene.Abstractions.Messages;

public interface IMessageDefinition
{
    ITopic Topic { get; }
    Type RequestType { get; }
}

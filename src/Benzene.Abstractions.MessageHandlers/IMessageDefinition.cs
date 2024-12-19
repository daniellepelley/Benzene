namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageDefinition
{
    string Topic { get; }
    Type RequestType { get; }
}

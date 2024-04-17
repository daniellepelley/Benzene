namespace Benzene.Abstractions.MessageHandling;

public interface IMessageDefinition
{
    string Topic { get; }
    Type RequestType { get; }
}

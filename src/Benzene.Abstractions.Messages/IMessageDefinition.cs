using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageDefinition
{
    ITopic Topic { get; }
    Type RequestType { get; }
}

using Benzene.Abstractions.Messages;

namespace Benzene.Extras.Broadcast;

public interface IBroadcastEventChecker : IMessageDefinitionFinder<IMessageDefinition>
{
    bool Check<T>(string topic, T payload);
}
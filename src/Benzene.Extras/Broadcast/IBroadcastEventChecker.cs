using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Extras.Broadcast;

public interface IBroadcastEventChecker : IMessageDefinitionFinder<IMessageDefinition>
{
    bool Check<T>(string topic, T payload);
}
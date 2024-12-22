using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.Broadcast;

public interface IBroadcastEventChecker : IMessageDefinitionFinder<IMessageDefinition>
{
    bool Check<T>(string topic, T payload);
}
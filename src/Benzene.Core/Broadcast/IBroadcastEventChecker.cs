using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.Broadcast;

public interface IBroadcastEventChecker : IMessageFinder<IMessageDefinition>
{
    bool Check<T>(string topic, T payload);
}
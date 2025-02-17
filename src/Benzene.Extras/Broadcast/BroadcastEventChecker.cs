using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Extras.Broadcast;

public class BroadcastEventChecker : IBroadcastEventChecker
{
    private readonly IMessageDefinition[] _messageDefinitions;

    public BroadcastEventChecker(params IMessageDefinition[] messageDefinitions)
    {
        _messageDefinitions = messageDefinitions;
    }

    public bool Check<T>(string topic, T payload)
    {
        return _messageDefinitions.Any(x => x.Topic.Id == topic && typeof(T) == x.RequestType);
    }

    public IMessageDefinition[] FindDefinitions() => _messageDefinitions;
}
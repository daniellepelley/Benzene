using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Extras.Broadcast;

public class BroadcastEventDefinition : IMessageDefinition
{
    public BroadcastEventDefinition(string topic, Type payloadType)
        :this(new Topic(topic), payloadType)
    { }
    
    public BroadcastEventDefinition(ITopic topic, Type payloadType)
    {
        Topic = topic;
        RequestType = payloadType;
    }


    public ITopic Topic { get; init; }
    public Type RequestType { get; init; }
}
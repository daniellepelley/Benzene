using System;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Core.Broadcast;

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
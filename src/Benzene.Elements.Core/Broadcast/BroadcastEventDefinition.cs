using System;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Elements.Core.Broadcast;

public class BroadcastEventDefinition : IMessageDefinition
{
    public BroadcastEventDefinition(string topic, Type payloadType)
    {
        Topic = topic;
        RequestType = payloadType;
    }

    public string Topic { get; init; }
    public Type RequestType { get; init; }
}
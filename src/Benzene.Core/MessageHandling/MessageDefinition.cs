using System;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandling;

public class MessageDefinition : IMessageDefinition
{
    public MessageDefinition(string topic, Type requestType)
    {
        Topic = topic;
        RequestType = requestType;
    }

    public string Topic { get; init; }
    public Type RequestType { get; init; }
}

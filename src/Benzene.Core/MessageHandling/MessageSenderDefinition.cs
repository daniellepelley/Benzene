using System;
using Benzene.Abstractions.MessageHandling;
using Void = Benzene.Results.Void;

namespace Benzene.Core.MessageHandling;

public class MessageSenderDefinition : IMessageSenderDefinition
{
    private MessageSenderDefinition(string topic, string version, Type requestType, Type responseType, Type senderType)
    {
        Topic = topic;
        Version = version;
        RequestType = requestType;
        ResponseType = responseType;
        SenderType = senderType;
    }

    public static MessageSenderDefinition CreateInstance(string topic, string version, Type requestType, Type responseType, Type senderType)
    {
        return new MessageSenderDefinition(topic, version, requestType, responseType, senderType);
    }
    
    public static MessageSenderDefinition CreateInstance(string topic, Type requestType, Type responseType, Type senderType)
    {
        return new MessageSenderDefinition(topic, string.Empty, requestType, responseType, senderType);
    }

    public static MessageSenderDefinition CreateInstance(string topic, Type requestType, Type responseType)
    {
        return new MessageSenderDefinition(topic, string.Empty, requestType, responseType, typeof(Void));
    }

    public static MessageSenderDefinition CreateInstance(string topic, Type requestType)
    {
        return new MessageSenderDefinition(topic, string.Empty, requestType, typeof(Void),typeof(Void));
    }

    public static MessageSenderDefinition Empty()
    {
        return new MessageSenderDefinition(Constants.Missing, string.Empty, typeof(Void), typeof(Void), typeof(Void));
    }

    public string Topic { get; init; }
    public string Version { get; init; }
    public Type RequestType { get; init; }
    public Type ResponseType { get; init; }
    public Type SenderType { get; init; }

}

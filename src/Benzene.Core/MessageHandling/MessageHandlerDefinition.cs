using System;
using Benzene.Abstractions.MessageHandling;
using Void = Benzene.Results.Void;

namespace Benzene.Core.MessageHandling;

public class MessageHandlerDefinition : IMessageHandlerDefinition
{
    private MessageHandlerDefinition(string topic, string version, Type requestType, Type responseType, Type handlerType)
    {
        Topic = topic;
        Version = version;
        RequestType = requestType;
        ResponseType = responseType;
        HandlerType = handlerType;
    }

    public static MessageHandlerDefinition CreateInstance(string topic, string version, Type requestType, Type responseType, Type handlerType)
    {
        return new MessageHandlerDefinition(topic, version, requestType, responseType, handlerType);
    }
    
    public static MessageHandlerDefinition CreateInstance(string topic, Type requestType, Type responseType, Type handlerType)
    {
        return new MessageHandlerDefinition(topic, string.Empty, requestType, responseType, handlerType);
    }

    public static MessageHandlerDefinition CreateInstance(string topic, Type requestType, Type responseType)
    {
        return new MessageHandlerDefinition(topic, string.Empty, requestType, responseType, typeof(Void));
    }

    public static MessageHandlerDefinition Empty()
    {
        return new MessageHandlerDefinition(Constants.Missing, string.Empty, typeof(Void), typeof(Void), typeof(Void));
    }

    public string Topic { get; init; }
    public string Version { get; init; }
    public Type RequestType { get; init; }
    public Type ResponseType { get; init; }
    public Type HandlerType { get; init; }

}

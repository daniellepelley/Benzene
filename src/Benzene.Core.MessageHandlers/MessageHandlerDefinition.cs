using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerDefinition : IMessageHandlerDefinition
{
    private MessageHandlerDefinition(ITopic topic, Type requestType, Type responseType, Type handlerType)
    {
        Topic = topic;
        RequestType = requestType;
        ResponseType = responseType;
        HandlerType = handlerType;
    }

    public static MessageHandlerDefinition CreateInstance(string topic, string version, Type requestType, Type responseType, Type handlerType)
    {
        return new MessageHandlerDefinition(new Topic(topic, version), requestType, responseType, handlerType);
    }
    
    public static MessageHandlerDefinition CreateInstance(string topic, Type requestType, Type responseType, Type handlerType)
    {
        return new MessageHandlerDefinition(new Topic(topic), requestType, responseType, handlerType);
    }

    public static MessageHandlerDefinition CreateInstance(string topic, Type requestType, Type responseType)
    {
        return new MessageHandlerDefinition(new Topic(topic), requestType, responseType, typeof(Void));
    }

    public static MessageHandlerDefinition Empty()
    {
        return new MessageHandlerDefinition(new Topic(Constants.Missing.Id), typeof(Void), typeof(Void), typeof(Void));
    }

    public ITopic Topic { get; init; }
    public Type RequestType { get; init; }
    public Type ResponseType { get; init; }
    public Type HandlerType { get; init; }

}

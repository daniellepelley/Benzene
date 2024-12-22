using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Abstractions.Results;
public interface IMessageHandlerResult
{
    ITopic? Topic { get; }
    IMessageHandlerDefinition? MessageHandlerDefinition { get; }
    IResult Result { get; }
}

public class MessageHandlerResult : IMessageHandlerResult
{
    public MessageHandlerResult(ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition, IResult result)
    {
        Topic = topic;
        MessageHandlerDefinition = messageHandlerDefinition;
        Result = result;
    }
    
    public MessageHandlerResult(IResult result)
    {
        Result = result;
    }

    public ITopic? Topic { get; }
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }
    public IResult Result { get; }
}

public interface IMessageResult
{
    ITopic Topic { get; }
    IMessageHandlerDefinition MessageHandlerDefinition { get; }
    string Status { get; }
    bool IsSuccessful { get; }
    object Payload { get; }
    string[] Errors { get; }
}
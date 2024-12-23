using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerResult : IMessageHandlerResult
{
    public MessageHandlerResult(ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition, IBenzeneResult benzeneResult)
    {
        Topic = topic;
        MessageHandlerDefinition = messageHandlerDefinition;
        BenzeneResult = benzeneResult;
    }
    
    public MessageHandlerResult(IBenzeneResult benzeneResult)
    {
        BenzeneResult = benzeneResult;
    }

    public ITopic? Topic { get; }
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }
    public IBenzeneResult BenzeneResult { get; }
}

public class MessageHandlerResult<TResponse> : IMessageHandlerResult<TResponse>
{
    public MessageHandlerResult(ITopic? topic, IMessageHandlerDefinition? messageHandlerDefinition, IBenzeneResult<TResponse> benzeneResult)
    {
        Topic = topic;
        MessageHandlerDefinition = messageHandlerDefinition;
        BenzeneResult = benzeneResult;
    }
    
    public MessageHandlerResult(IBenzeneResult<TResponse> benzeneResult)
    {
        BenzeneResult = benzeneResult;
    }

    public ITopic? Topic { get; }
    public IMessageHandlerDefinition? MessageHandlerDefinition { get; }
    public IBenzeneResult<TResponse> BenzeneResult { get; }
    public static explicit operator MessageHandlerResult(MessageHandlerResult<TResponse> messageHandlerResult)
        => new MessageHandlerResult(messageHandlerResult.Topic, messageHandlerResult.MessageHandlerDefinition, messageHandlerResult.BenzeneResult);

}
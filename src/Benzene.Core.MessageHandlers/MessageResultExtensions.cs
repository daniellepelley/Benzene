using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public static class MessageResultExtensions
{
    public static MessageResult AsMessageResult(this IResult serviceResult, ITopic topic)
    {
        return new MessageResult(topic, null, serviceResult.Status, serviceResult.IsSuccessful, serviceResult.PayloadAsObject, serviceResult.Errors);
    }

    public static MessageResult AsMessageResult(this IResult serviceResult, ITopic topic, IMessageHandlerDefinition messageHandlerDefinition)
    {
        return new MessageResult(topic, messageHandlerDefinition, serviceResult.Status, serviceResult.IsSuccessful, serviceResult.PayloadAsObject, serviceResult.Errors);
    }
}

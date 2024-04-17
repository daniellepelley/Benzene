using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Results;

namespace Benzene.Core;

public static class MessageResultExtensions
{
    public static MessageResult AsMessageResult(this IServiceResult serviceResult, string topic)
    {
        return new MessageResult(topic, null, serviceResult.Status, serviceResult.IsSuccessful, serviceResult.PayloadAsObject, serviceResult.Errors);
    }

    public static MessageResult AsMessageResult(this IServiceResult serviceResult, string topic, IMessageHandlerDefinition messageHandlerDefinition)
    {
        return new MessageResult(topic, messageHandlerDefinition, serviceResult.Status, serviceResult.IsSuccessful, serviceResult.PayloadAsObject, serviceResult.Errors);
    }
}

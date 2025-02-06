using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers.Response;

public class DefaultResponsePayloadMapper<TContext> : IResponsePayloadMapper<TContext>
{
    public string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
    {
        if (messageHandlerResult.MessageHandlerDefinition == null)

        {
            return null;
        }
        
        return messageHandlerResult.BenzeneResult.IsSuccessful
            ? SerializePayload(messageHandlerResult.MessageHandlerDefinition.ResponseType, messageHandlerResult.BenzeneResult.PayloadAsObject, serializer)
            : serializer.Serialize(AsErrorPayload(messageHandlerResult.BenzeneResult));
    }

    private static ErrorPayload AsErrorPayload(IBenzeneResult benzeneResult)
    {
        return new ErrorPayload(benzeneResult.Status, benzeneResult.Errors);
    }

    private string SerializePayload(Type type, object payload, ISerializer serializer)
    {
        if (payload == null)
        {
            return null;
        }
           // if (payload is IRawJsonMessage rawJsonMessage)
            // {
                // return CamelCaseJson(rawJsonMessage.Json);
            // }

            if (payload is IRawStringMessage rawStringMessage)
            {
                return rawStringMessage.Content;
            }

            return serializer.Serialize(type, payload);
    }
}

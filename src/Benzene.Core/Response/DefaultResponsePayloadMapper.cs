using System;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Core.Response;

public class DefaultResponsePayloadMapper<TContext> : IResponsePayloadMapper<TContext>
{
    public string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
    {
        if (messageHandlerResult.MessageHandlerDefinition == null)

        {
            return null;
        }
        
        return messageHandlerResult.Result.IsSuccessful
            ? SerializePayload(messageHandlerResult.MessageHandlerDefinition.ResponseType, messageHandlerResult.Result.PayloadAsObject, serializer)
            : serializer.Serialize(AsErrorPayload(messageHandlerResult.Result));
    }

    private static ErrorPayload AsErrorPayload(IResult result)
    {
        return new ErrorPayload(result.Status, result.Errors);
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

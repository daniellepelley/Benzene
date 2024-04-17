using System;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Core.Response;

public class DefaultResponsePayloadMapper<TContext> : IResponsePayloadMapper<TContext> where TContext : IHasMessageResult
{
    public string Map(TContext context, ISerializer serializer)
    {
        return context.MessageResult.IsSuccessful
            ? SerializePayload(context.MessageResult.MessageHandlerDefinition.ResponseType, context.MessageResult.Payload, serializer)
            : serializer.Serialize(AsErrorPayload(context.MessageResult));
    }

    private static ErrorPayload AsErrorPayload(IMessageResult messageResult)
    {
        return new ErrorPayload(messageResult.Status, messageResult.Errors);
    }

    private string SerializePayload(Type type, object payload, ISerializer serializer)
    {
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

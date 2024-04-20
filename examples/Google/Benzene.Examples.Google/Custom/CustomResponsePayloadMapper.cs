﻿using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Results;

namespace Benzene.Examples.Google.Custom;

public class CustomResponsePayloadMapper<TContext> : IResponsePayloadMapper<TContext> where TContext : IHasMessageResult
{
    private readonly IMessageHandlersLookUp _messageHandlersLookUp;

    public CustomResponsePayloadMapper(IMessageHandlersLookUp messageHandlersLookUp)
    {
        _messageHandlersLookUp = messageHandlersLookUp;
    }

    public string Map(TContext context, ISerializer serializer)
    {
        return MapBodyString(context, serializer);
    }

    private static string TopicFunction(IMessageResult messageResult)
    {
        return messageResult.Topic.Split("_").Last();
    }

    private static ErrorPayload AsErrorPayload(IMessageResult messageResult)
    {
        return new ErrorPayload(messageResult.Status, messageResult.Errors);
    }

    private string MapBodyString(TContext context, ISerializer serializer)
    {
        var messageResult = context.MessageResult;
        var topicFunction = TopicFunction(messageResult);
        if (topicFunction == "create" && messageResult.Payload is IHasId hasId)
        {
            return messageResult.IsSuccessful
                ? serializer.Serialize(hasId.Id.ToString())
                : serializer.Serialize(AsErrorPayload(messageResult));
        }

        if (topicFunction == "create" && messageResult.Payload is IHasId<string> hasIdString)
        {
            return messageResult.IsSuccessful
                ? serializer.Serialize(hasIdString.Id)
                : serializer.Serialize(AsErrorPayload(messageResult));
        }

        if (topicFunction is "update" or "delete")
        {
            return messageResult.IsSuccessful
                ? serializer.Serialize<NullPayload>(null)
                : serializer.Serialize(AsErrorPayload(messageResult));
        }
        return messageResult.IsSuccessful
            ? serializer.Serialize(messageResult.MessageHandlerDefinition.ResponseType, messageResult.Payload)
            : serializer.Serialize(AsErrorPayload(messageResult));
    }
}
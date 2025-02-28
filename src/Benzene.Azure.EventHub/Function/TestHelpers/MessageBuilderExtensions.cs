﻿using Azure.Messaging.EventHubs;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages.BenzeneMessage.TestHelpers;

namespace Benzene.Azure.EventHub.Function.TestHelpers;

public static class MessageBuilderExtensions
{
    public static EventData AsEventHubBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsEventHubBenzeneMessage(new JsonSerializer());
    }

    public static EventData AsEventHubBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        return new EventData
        {
            EventBody = new BinaryData(source.AsBenzeneMessage(serializer))
        };
    }

}

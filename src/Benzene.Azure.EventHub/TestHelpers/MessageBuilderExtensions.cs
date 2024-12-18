using Azure.Messaging.EventHubs;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.BenzeneMessage.TestHelpers;
using Benzene.Core.Serialization;

namespace Benzene.Azure.EventHub.TestHelpers;

public static class MessageBuilderExtensions
{
    public static EventData AsEventHubBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return AsEventHubBenzeneMessage(source, new JsonSerializer());
    }
    
    public static EventData AsEventHubBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        return new EventData
        {
            EventBody = new BinaryData(source.AsBenzeneMessage(serializer))
        };
    }

}

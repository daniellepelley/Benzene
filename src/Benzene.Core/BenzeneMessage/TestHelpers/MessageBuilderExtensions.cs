using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Serialization;

namespace Benzene.Core.BenzeneMessage.TestHelpers;

public static class MessageBuilderExtensions
{
    public static BenzeneMessageRequest AsBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return AsBenzeneMessage(source, new JsonSerializer());
    }

    public static BenzeneMessageRequest AsBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        return new BenzeneMessageRequest
        {
            Topic = source.Topic,
            Body = serializer.Serialize(source.Message),
            Headers = source.Headers
        };
    }

}

using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Messages.BenzeneMessage.TestHelpers;

public static class MessageBuilderExtensions
{
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

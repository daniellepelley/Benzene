using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.Messages.TestHelpers;

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

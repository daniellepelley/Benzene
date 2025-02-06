using Benzene.Abstractions;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Messages.BenzeneMessage.TestHelpers;

namespace Benzene.Core.MessageHandlers.BenzeneMessage.TestHelpers;

public static class MessageBuilderExtensions
{
    public static BenzeneMessageRequest AsBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsBenzeneMessage(new JsonSerializer());
    }
}

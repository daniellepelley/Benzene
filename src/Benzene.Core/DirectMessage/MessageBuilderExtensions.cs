using Benzene.Abstractions;

namespace Benzene.Core.DirectMessage;

public static class MessageBuilderExtensions
{
    public static DirectMessageRequest AsDirectMessage(this IMessageBuilder source)
    {
        return new DirectMessageRequest
        {
            Topic = source.Topic,
            Message = System.Text.Json.JsonSerializer.Serialize(source.Message),
            Headers = source.Headers
        };
    }
}

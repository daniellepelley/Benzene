using Benzene.Abstractions;

namespace Benzene.Core.BenzeneMessage.TestHelpers;

public static class MessageBuilderExtensions
{
    public static BenzeneMessageRequest AsBenzeneMessage(this IMessageBuilder source)
    {
        return new BenzeneMessageRequest
        {
            Topic = source.Topic,
            Body = System.Text.Json.JsonSerializer.Serialize(source.Message),
            Headers = source.Headers
        };
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Aws.Lambda.EventBridge.TestHelpers;

public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds a realistic EventBridge event from the message builder: the topic becomes
    /// <c>detail-type</c>, the message becomes <c>detail</c>, and any headers are embedded under the
    /// reserved <c>_benzeneHeaders</c> key — the same shape the outbound
    /// <c>EventBridgeBenzeneMessageClient</c> publishes.
    /// </summary>
    public static EventBridgeEvent AsEventBridge<T>(this IMessageBuilder<T> source, string eventSource = "benzene.test")
    {
        return AsEventBridge(source, new JsonSerializer(), eventSource);
    }

    public static EventBridgeEvent AsEventBridge<T>(this IMessageBuilder<T> source, ISerializer serializer, string eventSource = "benzene.test")
    {
        var json = serializer.Serialize(source.Message);

        if (source.Headers.Any() && JsonNode.Parse(json) is JsonObject detailObject)
        {
            var embedded = new JsonObject();
            foreach (var header in source.Headers)
            {
                embedded[header.Key] = header.Value;
            }

            detailObject["_benzeneHeaders"] = embedded;
            json = detailObject.ToJsonString();
        }

        using var document = JsonDocument.Parse(json);

        return new EventBridgeEvent
        {
            Version = "0",
            Id = Guid.NewGuid().ToString(),
            DetailType = source.Topic,
            Source = eventSource,
            Account = "123456789012",
            Time = "2026-01-01T00:00:00Z",
            Region = "eu-west-1",
            Resources = new List<string>(),
            Detail = document.RootElement.Clone()
        };
    }
}

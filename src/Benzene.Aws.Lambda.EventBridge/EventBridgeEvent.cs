using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// The Amazon EventBridge event envelope, as delivered to a Lambda target (one event per invocation).
/// Modeled as Benzene's own type — the envelope shape is stable and documented, and this keeps the
/// package free of any extra AWS event-package dependency. <see cref="Detail"/> is kept as a raw
/// <see cref="JsonElement"/>: it's the domain payload, handed to the normal request-mapping pipeline.
/// </summary>
public class EventBridgeEvent
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>The event's routing key — mapped to the Benzene message topic (plan decision E1).</summary>
    [JsonPropertyName("detail-type")]
    public string DetailType { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("account")]
    public string Account { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; }

    [JsonPropertyName("resources")]
    public List<string> Resources { get; set; }

    /// <summary>The domain payload (a JSON object for routable events).</summary>
    [JsonPropertyName("detail")]
    public JsonElement Detail { get; set; }
}

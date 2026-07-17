using System.Text.Json;

namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Benzene's own model of an Event Grid delivery - dependency-free (BCL <see cref="JsonElement"/>
/// for the payload, no <c>Azure.Messaging.EventGrid</c>), mirroring how
/// <c>Benzene.Aws.Lambda.Kinesis</c>/<c>EventBridge</c> model their Lambda events. Covers both
/// wire schemas Event Grid can deliver: the Event Grid schema (<c>eventType</c>/<c>topic</c>) and
/// CloudEvents 1.0 (<c>type</c>/<c>source</c>, detected by <c>specversion</c>) - see
/// <see cref="Parse"/>.
/// </summary>
public class EventGridTriggerEvent
{
    /// <summary>The event's unique id.</summary>
    public string? Id { get; init; }

    /// <summary>
    /// The event type (Event Grid schema <c>eventType</c>, CloudEvents <c>type</c>) - e.g.
    /// <c>Microsoft.Storage.BlobCreated</c> or your own custom type. This is also the topic the
    /// event routes by.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>The publisher-defined subject path (both schemas' <c>subject</c>).</summary>
    public string? Subject { get; init; }

    /// <summary>
    /// The full resource path of the event source (Event Grid schema <c>topic</c>, CloudEvents
    /// <c>source</c>). Named to avoid colliding with Benzene's own routing notion of "topic",
    /// which is <see cref="EventType"/> here.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>When the event was generated (Event Grid schema <c>eventTime</c>, CloudEvents <c>time</c>).</summary>
    public DateTimeOffset? EventTime { get; init; }

    /// <summary>The schema version of the data object (Event Grid schema only).</summary>
    public string? DataVersion { get; init; }

    /// <summary>The event's payload, as raw JSON (both schemas' <c>data</c>).</summary>
    public JsonElement? Data { get; init; }

    /// <summary>
    /// Parses a raw delivery - the <c>[EventGridTrigger] string</c> binding - into an
    /// <see cref="EventGridTriggerEvent"/>, handling both the Event Grid schema and CloudEvents 1.0
    /// (detected by the presence of <c>specversion</c>).
    /// </summary>
    /// <param name="json">The event JSON as delivered to the trigger.</param>
    /// <returns>The parsed event.</returns>
    public static EventGridTriggerEvent Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var isCloudEvent = root.TryGetProperty("specversion", out _);

        return new EventGridTriggerEvent
        {
            Id = GetString(root, "id"),
            EventType = isCloudEvent ? GetString(root, "type") : GetString(root, "eventType"),
            Subject = GetString(root, "subject"),
            Source = isCloudEvent ? GetString(root, "source") : GetString(root, "topic"),
            EventTime = GetTime(root, isCloudEvent ? "time" : "eventTime"),
            DataVersion = GetString(root, "dataVersion"),
            Data = root.TryGetProperty("data", out var data) ? data.Clone() : null
        };
    }

    private static string? GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTimeOffset? GetTime(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
               && value.TryGetDateTimeOffset(out var time)
            ? time
            : null;
    }
}

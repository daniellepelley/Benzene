using System.Globalization;
using System.Text.Json;
using Benzene.Mesh.Wire;

namespace Benzene.Mesh.Fleet.Tempo;

/// <summary>
/// Maps Grafana Tempo's trace-by-id response (OTLP/JSON — the shape <c>GET /api/traces/{id}</c> returns
/// with <c>Accept: application/json</c>) into the mesh's <see cref="MeshTraceEvent"/> shape. Only spans
/// carrying a Benzene topic attribute become events — a real trace mixes in transport/SDK spans the mesh
/// view has no place for, so this filters to the topic-bearing spans the pipeline stamps
/// (<c>Benzene.Diagnostics.ActivityMiddlewareDecorator</c>; see <c>work/otel-fleet-adapter-scope.md</c>).
/// </summary>
/// <remarks>
/// The Benzene span attributes are read by their dotted OTLP names verbatim (<c>benzene.topic</c>,
/// <c>benzene.status</c>, <c>benzene.version</c>, <c>benzene.correlation-id</c>) — Tempo preserves
/// attribute keys as emitted, so unlike the X-Ray adapter there is no annotation/metadata key-sanitising
/// to reconcile. The emitting service is the batch's <c>resource</c> <c>service.name</c>. Handles both
/// the current <c>scopeSpans</c> and the legacy <c>instrumentationLibrarySpans</c> batch shape.
/// </remarks>
public static class TempoTraceMapper
{
    /// <summary>Parse a Tempo trace-by-id OTLP/JSON body and emit one <see cref="MeshTraceEvent"/> per
    /// topic-bearing span, carrying the queried <paramref name="meshTraceId"/> and ordered by start time.
    /// A body that fails to parse yields an empty list (traces are read best-effort).</summary>
    public static List<MeshTraceEvent> Map(string meshTraceId, string body)
    {
        var events = new List<MeshTraceEvent>();
        if (string.IsNullOrWhiteSpace(body))
        {
            return events;
        }

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return events;
        }

        using (parsed)
        {
            if (!parsed.RootElement.TryGetProperty("batches", out var batches) || batches.ValueKind != JsonValueKind.Array)
            {
                return events;
            }

            foreach (var batch in batches.EnumerateArray())
            {
                var service = ResourceServiceName(batch);
                foreach (var span in SpansIn(batch))
                {
                    var attributes = ReadAttributes(span);
                    if (!attributes.TryGetValue("benzene.topic", out var topic) || string.IsNullOrEmpty(topic))
                    {
                        continue;
                    }

                    events.Add(new MeshTraceEvent
                    {
                        TraceId = meshTraceId,
                        SpanId = GetString(span, "spanId") ?? string.Empty,
                        ParentSpanId = EmptyToNull(GetString(span, "parentSpanId")),
                        Service = service,
                        Topic = topic,
                        TopicVersion = attributes.GetValueOrDefault("benzene.version"),
                        Status = attributes.GetValueOrDefault("benzene.status") ?? string.Empty,
                        CorrelationId = attributes.GetValueOrDefault("benzene.correlation-id"),
                        StartedAt = NanosToTime(GetString(span, "startTimeUnixNano")),
                        DurationMs = DurationMs(span)
                    });
                }
            }
        }

        events.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
        return events;
    }

    private static string? ResourceServiceName(JsonElement batch)
    {
        if (batch.TryGetProperty("resource", out var resource))
        {
            var attributes = ReadAttributes(resource);
            if (attributes.TryGetValue("service.name", out var name) && !string.IsNullOrEmpty(name))
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>Every span in a batch, across the current <c>scopeSpans</c> and legacy
    /// <c>instrumentationLibrarySpans</c> shapes.</summary>
    private static IEnumerable<JsonElement> SpansIn(JsonElement batch)
    {
        foreach (var scopeKey in new[] { "scopeSpans", "instrumentationLibrarySpans" })
        {
            if (batch.TryGetProperty(scopeKey, out var scopes) && scopes.ValueKind == JsonValueKind.Array)
            {
                foreach (var scope in scopes.EnumerateArray())
                {
                    if (scope.TryGetProperty("spans", out var spans) && spans.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var span in spans.EnumerateArray())
                        {
                            yield return span;
                        }
                    }
                }
            }
        }
    }

    /// <summary>Read an OTLP <c>attributes</c> array (<c>[{ "key": k, "value": { "stringValue": v } }]</c>)
    /// into a flat key→string map, coercing non-string value kinds to their text form.</summary>
    private static Dictionary<string, string> ReadAttributes(JsonElement node)
    {
        var result = new Dictionary<string, string>();
        if (!node.TryGetProperty("attributes", out var attributes) || attributes.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var attribute in attributes.EnumerateArray())
        {
            var key = GetString(attribute, "key");
            if (string.IsNullOrEmpty(key) || !attribute.TryGetProperty("value", out var value))
            {
                continue;
            }

            var text = ReadAnyValue(value);
            if (text is not null)
            {
                result[key] = text;
            }
        }

        return result;
    }

    /// <summary>Read an OTLP <c>AnyValue</c> (<c>stringValue</c>/<c>intValue</c>/<c>boolValue</c>/
    /// <c>doubleValue</c>) as text.</summary>
    private static string? ReadAnyValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (value.TryGetProperty("stringValue", out var s) && s.ValueKind == JsonValueKind.String)
        {
            return s.GetString();
        }
        // intValue is JSON-encoded as a string per the OTLP/JSON spec; the others are JSON scalars.
        foreach (var key in new[] { "intValue", "boolValue", "doubleValue" })
        {
            if (value.TryGetProperty(key, out var element))
            {
                return element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
            }
        }

        return null;
    }

    private static double DurationMs(JsonElement span)
    {
        var start = ParseNanos(GetString(span, "startTimeUnixNano"));
        var end = ParseNanos(GetString(span, "endTimeUnixNano"));
        return end > start ? (end - start) / 1_000_000.0 : 0;
    }

    private static DateTimeOffset NanosToTime(string? nanos)
    {
        var value = ParseNanos(nanos);
        return value > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(value / 1_000_000) : default;
    }

    private static long ParseNanos(string? raw)
        => long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static string? GetString(JsonElement node, string name)
        => node.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? EmptyToNull(string? value) => string.IsNullOrEmpty(value) ? null : value;
}

using System.Text.Json;
using Benzene.Mesh.Wire;

namespace Benzene.Mesh.Fleet.Jaeger;

/// <summary>One trace as mapped from Jaeger: its id and the mesh events of its topic-bearing spans.</summary>
public readonly record struct JaegerMappedTrace(string TraceId, List<MeshTraceEvent> Events);

/// <summary>
/// Maps a Jaeger query response (<c>GET /api/traces/{id}</c> or <c>/api/traces?service=…</c>, both of
/// which return <c>{ "data": [ trace, … ] }</c>) into the mesh's <see cref="MeshTraceEvent"/> shape. Only
/// spans carrying a Benzene topic tag become events — a real trace mixes in transport/SDK spans the mesh
/// view has no place for, so this filters to the topic-bearing spans the pipeline stamps
/// (<c>Benzene.Diagnostics.ActivityMiddlewareDecorator</c>; see <c>work/otel-fleet-adapter-scope.md</c>).
/// </summary>
/// <remarks>
/// Jaeger's model differs from OTLP/Tempo: times are <b>microseconds</b> (<c>startTime</c>/<c>duration</c>),
/// parentage is a <c>references</c> entry with <c>refType == "CHILD_OF"</c> (not a <c>parentSpanId</c>
/// field), and the emitting service is <c>processes[processID].serviceName</c> (not a resource attribute).
/// Benzene tag keys are read by their dotted names verbatim (<c>benzene.topic</c> etc.) — Jaeger preserves
/// tag keys, so there is no key-sanitising to reconcile.
/// </remarks>
public static class JaegerTraceMapper
{
    /// <summary>Parse a Jaeger <c>data[]</c> response into one <see cref="JaegerMappedTrace"/> per trace,
    /// each carrying its topic-bearing spans in start order. A body that fails to parse yields an empty
    /// list (traces are read best-effort).</summary>
    public static List<JaegerMappedTrace> MapTraces(string body)
    {
        var results = new List<JaegerMappedTrace>();
        if (string.IsNullOrWhiteSpace(body))
        {
            return results;
        }

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return results;
        }

        using (parsed)
        {
            if (!parsed.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (var trace in data.EnumerateArray())
            {
                var mapped = MapTrace(trace);
                if (mapped is { } value)
                {
                    results.Add(value);
                }
            }
        }

        return results;
    }

    private static JaegerMappedTrace? MapTrace(JsonElement trace)
    {
        var traceId = GetString(trace, "traceID");
        if (string.IsNullOrEmpty(traceId) || !trace.TryGetProperty("spans", out var spans) || spans.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var services = ProcessServiceNames(trace);
        var events = new List<MeshTraceEvent>();

        foreach (var span in spans.EnumerateArray())
        {
            var tags = ReadTags(span);
            if (!tags.TryGetValue("benzene.topic", out var topic) || string.IsNullOrEmpty(topic))
            {
                continue;
            }

            var processId = GetString(span, "processID");
            events.Add(new MeshTraceEvent
            {
                TraceId = traceId!,
                SpanId = GetString(span, "spanID") ?? string.Empty,
                ParentSpanId = ChildOfParent(span),
                Service = processId is not null ? services.GetValueOrDefault(processId) : null,
                Topic = topic,
                TopicVersion = tags.GetValueOrDefault("benzene.version"),
                Status = tags.GetValueOrDefault("benzene.status") ?? string.Empty,
                CorrelationId = tags.GetValueOrDefault("benzene.correlation-id"),
                StartedAt = MicrosToTime(GetLong(span, "startTime")),
                DurationMs = GetLong(span, "duration") / 1000.0 // µs → ms
            });
        }

        events.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
        return new JaegerMappedTrace(traceId!, events);
    }

    private static Dictionary<string, string> ProcessServiceNames(JsonElement trace)
    {
        var result = new Dictionary<string, string>();
        if (trace.TryGetProperty("processes", out var processes) && processes.ValueKind == JsonValueKind.Object)
        {
            foreach (var process in processes.EnumerateObject())
            {
                var name = GetString(process.Value, "serviceName");
                if (!string.IsNullOrEmpty(name))
                {
                    result[process.Name] = name!;
                }
            }
        }

        return result;
    }

    /// <summary>The parent span id from the first <c>CHILD_OF</c> reference, or null (a root span).</summary>
    private static string? ChildOfParent(JsonElement span)
    {
        if (span.TryGetProperty("references", out var references) && references.ValueKind == JsonValueKind.Array)
        {
            foreach (var reference in references.EnumerateArray())
            {
                if (GetString(reference, "refType") == "CHILD_OF")
                {
                    var parent = GetString(reference, "spanID");
                    return string.IsNullOrEmpty(parent) ? null : parent;
                }
            }
        }

        return null;
    }

    /// <summary>Read a Jaeger <c>tags</c> array (<c>[{ "key": k, "type": t, "value": v }]</c>) into a
    /// key→string map, coercing non-string values to text.</summary>
    private static Dictionary<string, string> ReadTags(JsonElement span)
    {
        var result = new Dictionary<string, string>();
        if (!span.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var tag in tags.EnumerateArray())
        {
            var key = GetString(tag, "key");
            if (string.IsNullOrEmpty(key) || !tag.TryGetProperty("value", out var value))
            {
                continue;
            }

            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
                _ => null
            };
            if (text is not null)
            {
                result[key!] = text;
            }
        }

        return result;
    }

    private static DateTimeOffset MicrosToTime(long micros)
        => micros > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(micros / 1000) : default;

    private static string? GetString(JsonElement node, string name)
        => node.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static long GetLong(JsonElement node, string name)
        => node.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt64(out var value)
            ? value
            : 0;
}

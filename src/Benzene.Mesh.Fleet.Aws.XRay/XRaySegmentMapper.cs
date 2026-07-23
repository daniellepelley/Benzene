using System.Globalization;
using System.Text.Json;
using Benzene.Mesh.Wire;

namespace Benzene.Mesh.Fleet.Aws.XRay;

/// <summary>
/// Maps the raw X-Ray segment documents of one trace (the JSON strings X-Ray returns from
/// <c>BatchGetTraces</c>) into the mesh's <see cref="MeshTraceEvent"/> shape. Only nodes that carry a
/// Benzene topic attribute become events - X-Ray traces mix in transport/AWS-SDK spans the mesh view
/// has no place for, so this filters to the topic-bearing spans the pipeline stamps
/// (<c>Benzene.Diagnostics.ActivityMiddlewareDecorator</c> - see <c>work/otel-fleet-adapter-scope.md</c>).
/// </summary>
/// <remarks>
/// A Benzene span's attributes may land in X-Ray as either <c>annotations</c> (keys sanitised to
/// underscores - <c>benzene_topic</c>) or <c>metadata</c> (dotted keys preserved - <c>benzene.topic</c>,
/// possibly nested under a namespace like <c>default</c>), depending on how the OTel→X-Ray exporter is
/// configured. This reads both forms so the adapter works regardless of that choice. Segment/subsegment
/// nesting gives parentage; the enclosing segment's <c>name</c> is the emitting service.
/// </remarks>
public static class XRaySegmentMapper
{
    /// <summary>
    /// Parse each segment document and emit one <see cref="MeshTraceEvent"/> per topic-bearing node,
    /// carrying the queried <paramref name="meshTraceId"/> and ordered by start time. A document that
    /// fails to parse is skipped rather than failing the whole lookup (X-Ray traces are read
    /// best-effort). Returns an empty list when no node carried a Benzene topic.
    /// </summary>
    public static List<MeshTraceEvent> Map(string meshTraceId, IEnumerable<string> segmentDocuments)
    {
        var events = new List<MeshTraceEvent>();

        foreach (var document in segmentDocuments)
        {
            if (string.IsNullOrWhiteSpace(document))
            {
                continue;
            }

            JsonDocument parsed;
            try
            {
                parsed = JsonDocument.Parse(document);
            }
            catch (JsonException)
            {
                continue;
            }

            using (parsed)
            {
                var segment = parsed.RootElement;
                var service = TryGetString(segment, "name");
                Walk(meshTraceId, segment, service, events);
            }
        }

        events.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
        return events;
    }

    /// <summary>Recursively walk a segment/subsegment node, emitting an event for any topic-bearing node
    /// and descending into its subsegments.</summary>
    private static void Walk(string meshTraceId, JsonElement node, string? service, List<MeshTraceEvent> events)
    {
        var topic = ReadBenzene(node, "topic");
        if (!string.IsNullOrEmpty(topic))
        {
            events.Add(new MeshTraceEvent
            {
                TraceId = meshTraceId,
                SpanId = TryGetString(node, "id") ?? string.Empty,
                ParentSpanId = TryGetString(node, "parent_id"),
                Service = service,
                Topic = topic!,
                TopicVersion = ReadBenzene(node, "version"),
                Status = ReadBenzene(node, "status") ?? string.Empty,
                CorrelationId = ReadBenzene(node, "correlation-id") ?? ReadBenzene(node, "correlationId"),
                StartedAt = ReadStartedAt(node),
                DurationMs = ReadDurationMs(node)
            });
        }

        if (node.TryGetProperty("subsegments", out var subsegments)
            && subsegments.ValueKind == JsonValueKind.Array)
        {
            foreach (var sub in subsegments.EnumerateArray())
            {
                // Keep the enclosing segment's name as the service: subsegments are the same service's
                // internal spans, not a new service boundary (which X-Ray would model as its own segment).
                Walk(meshTraceId, sub, service, events);
            }
        }
    }

    /// <summary>Read a <c>benzene.&lt;name&gt;</c> attribute from either the node's annotations (underscore
    /// key) or its metadata (dotted key, at the top level or one namespace deep).</summary>
    private static string? ReadBenzene(JsonElement node, string name)
    {
        var underscoreKey = "benzene_" + name.Replace('-', '_').Replace('.', '_');
        var dottedKey = "benzene." + name;

        if (node.TryGetProperty("annotations", out var annotations)
            && annotations.ValueKind == JsonValueKind.Object
            && TryReadValue(annotations, underscoreKey, out var fromAnnotation))
        {
            return fromAnnotation;
        }

        if (node.TryGetProperty("metadata", out var metadata)
            && metadata.ValueKind == JsonValueKind.Object)
        {
            if (TryReadValue(metadata, dottedKey, out var fromMetadata))
            {
                return fromMetadata;
            }

            // OTel→X-Ray nests span attributes one namespace deep (default "default").
            foreach (var ns in metadata.EnumerateObject())
            {
                if (ns.Value.ValueKind == JsonValueKind.Object
                    && TryReadValue(ns.Value, dottedKey, out var fromNamespaced))
                {
                    return fromNamespaced;
                }
            }
        }

        return null;
    }

    private static bool TryReadValue(JsonElement obj, string key, out string? value)
    {
        if (obj.TryGetProperty(key, out var element))
        {
            value = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
            return value is not null;
        }

        value = null;
        return false;
    }

    /// <summary>X-Ray <c>start_time</c> is epoch seconds as a double (sub-second fraction preserved).</summary>
    private static DateTimeOffset ReadStartedAt(JsonElement node)
    {
        if (TryGetDouble(node, "start_time", out var startSeconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)(startSeconds * 1000));
        }

        return default;
    }

    private static double ReadDurationMs(JsonElement node)
    {
        if (TryGetDouble(node, "start_time", out var start) && TryGetDouble(node, "end_time", out var end))
        {
            return (end - start) * 1000;
        }

        return 0;
    }

    private static string? TryGetString(JsonElement node, string name)
        => node.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static bool TryGetDouble(JsonElement node, string name, out double value)
    {
        if (node.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number)
        {
            value = element.GetDouble();
            return true;
        }

        // Some exporters serialise the epoch as a string; accept that too.
        if (element.ValueKind == JsonValueKind.String
            && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }
}

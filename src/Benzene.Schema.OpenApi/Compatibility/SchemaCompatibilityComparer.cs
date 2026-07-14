using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// Compares two versions of a service's schema (the client's generation-time
/// <see cref="EventServiceDocument"/> against the service's current one) and produces a
/// <see cref="SchemaCompatibilityReport"/> classifying each difference as compatible, a warning, or
/// breaking. Unlike a single schema hash — which only tells you <em>whether</em> anything changed —
/// this tells you <em>what</em> changed and whether it actually breaks the contract.
/// </summary>
public class SchemaCompatibilityComparer
{
    private const int MaxDepth = 32;

    private readonly SchemaCompatibilityRules _rules;

    public SchemaCompatibilityComparer(SchemaCompatibilityRules? rules = null)
    {
        _rules = rules ?? SchemaCompatibilityRules.Default();
    }

    /// <summary>
    /// Compares <paramref name="baseline"/> (what the client was generated against) with
    /// <paramref name="current"/> (the service's current schema).
    /// </summary>
    public SchemaCompatibilityReport Compare(EventServiceDocument baseline, EventServiceDocument current)
    {
        var changes = new List<SchemaChange>();

        CompareRequests(baseline, current, changes);
        CompareEvents(baseline, current, changes);

        return new SchemaCompatibilityReport(changes);
    }

    private void CompareRequests(EventServiceDocument baseline, EventServiceDocument current, List<SchemaChange> changes)
    {
        var baselineByKey = Index(baseline.Requests, RequestKey);
        var currentByKey = Index(current.Requests, RequestKey);

        foreach (var entry in baselineByKey)
        {
            if (!currentByKey.ContainsKey(entry.Key))
            {
                changes.Add(Change(SchemaChangeKind.TopicRemoved, SchemaDirection.Request, entry.Value.Topic,
                    entry.Value.Topic, $"Topic '{entry.Value.Topic}' is no longer served"));
            }
        }

        foreach (var entry in currentByKey)
        {
            if (!baselineByKey.ContainsKey(entry.Key))
            {
                changes.Add(Change(SchemaChangeKind.TopicAdded, SchemaDirection.Request, entry.Value.Topic,
                    entry.Value.Topic, $"Topic '{entry.Value.Topic}' was added"));
            }
        }

        foreach (var entry in baselineByKey)
        {
            if (currentByKey.TryGetValue(entry.Key, out var currentReq))
            {
                CompareSchemas(Resolve(entry.Value.Request, baseline), Resolve(currentReq.Request, current),
                    baseline, current, SchemaDirection.Request, entry.Value.Topic, $"{entry.Value.Topic}.request", changes, 0);
                CompareSchemas(Resolve(entry.Value.Response, baseline), Resolve(currentReq.Response, current),
                    baseline, current, SchemaDirection.Response, entry.Value.Topic, $"{entry.Value.Topic}.response", changes, 0);
            }
        }
    }

    private void CompareEvents(EventServiceDocument baseline, EventServiceDocument current, List<SchemaChange> changes)
    {
        var baselineByKey = Index(baseline.Events, e => e.Topic);
        var currentByKey = Index(current.Events, e => e.Topic);

        foreach (var entry in baselineByKey)
        {
            if (!currentByKey.ContainsKey(entry.Key))
            {
                changes.Add(Change(SchemaChangeKind.TopicRemoved, SchemaDirection.Event, entry.Value.Topic,
                    entry.Value.Topic, $"Event '{entry.Value.Topic}' is no longer published"));
            }
        }

        foreach (var entry in currentByKey)
        {
            if (!baselineByKey.ContainsKey(entry.Key))
            {
                changes.Add(Change(SchemaChangeKind.TopicAdded, SchemaDirection.Event, entry.Value.Topic,
                    entry.Value.Topic, $"Event '{entry.Value.Topic}' was added"));
            }
        }

        foreach (var entry in baselineByKey)
        {
            if (currentByKey.TryGetValue(entry.Key, out var currentEvent))
            {
                CompareSchemas(Resolve(entry.Value.Message, baseline), Resolve(currentEvent.Message, current),
                    baseline, current, SchemaDirection.Event, entry.Value.Topic, $"{entry.Value.Topic}.message", changes, 0);
            }
        }
    }

    private void CompareSchemas(OpenApiSchema? baseline, OpenApiSchema? current,
        EventServiceDocument baselineDoc, EventServiceDocument currentDoc, SchemaDirection direction,
        string topic, string path, List<SchemaChange> changes, int depth)
    {
        if (baseline == null || current == null || depth > MaxDepth)
        {
            return;
        }

        if (baseline.Type != current.Type || baseline.Format != current.Format)
        {
            changes.Add(Change(SchemaChangeKind.TypeChanged, direction, topic, path,
                $"Type changed from '{Describe(baseline)}' to '{Describe(current)}'"));
            return; // fundamentally different types — no point diffing their members
        }

        var baselineProps = baseline.Properties ?? new Dictionary<string, OpenApiSchema>();
        var currentProps = current.Properties ?? new Dictionary<string, OpenApiSchema>();
        var baselineRequired = baseline.Required ?? new HashSet<string>();
        var currentRequired = current.Required ?? new HashSet<string>();

        foreach (var prop in baselineProps)
        {
            if (!currentProps.ContainsKey(prop.Key))
            {
                changes.Add(Change(SchemaChangeKind.PropertyRemoved, direction, topic, $"{path}.{prop.Key}",
                    $"Property '{prop.Key}' was removed"));
            }
        }

        foreach (var prop in currentProps)
        {
            if (!baselineProps.ContainsKey(prop.Key))
            {
                var isRequired = currentRequired.Contains(prop.Key);
                var kind = isRequired ? SchemaChangeKind.RequiredPropertyAdded : SchemaChangeKind.PropertyAdded;
                changes.Add(Change(kind, direction, topic, $"{path}.{prop.Key}",
                    $"Property '{prop.Key}' was added{(isRequired ? " (required)" : "")}"));
            }
        }

        foreach (var prop in baselineProps)
        {
            if (!currentProps.TryGetValue(prop.Key, out var currentPropSchema))
            {
                continue;
            }

            var wasRequired = baselineRequired.Contains(prop.Key);
            var isRequired = currentRequired.Contains(prop.Key);

            if (!wasRequired && isRequired)
            {
                changes.Add(Change(SchemaChangeKind.PropertyBecameRequired, direction, topic, $"{path}.{prop.Key}",
                    $"Property '{prop.Key}' became required"));
            }
            else if (wasRequired && !isRequired)
            {
                changes.Add(Change(SchemaChangeKind.PropertyBecameOptional, direction, topic, $"{path}.{prop.Key}",
                    $"Property '{prop.Key}' became optional"));
            }

            CompareSchemas(Resolve(prop.Value, baselineDoc), Resolve(currentPropSchema, currentDoc),
                baselineDoc, currentDoc, direction, topic, $"{path}.{prop.Key}", changes, depth + 1);
        }

        if (baseline.Items != null && current.Items != null)
        {
            CompareSchemas(Resolve(baseline.Items, baselineDoc), Resolve(current.Items, currentDoc),
                baselineDoc, currentDoc, direction, topic, $"{path}[]", changes, depth + 1);
        }
    }

    /// <summary>Follows a <c>$ref</c> into the document's components, or returns the schema unchanged.</summary>
    private static OpenApiSchema? Resolve(OpenApiSchema? schema, EventServiceDocument doc)
    {
        if (schema == null)
        {
            return null;
        }

        var id = schema.Reference?.Id;
        if (!string.IsNullOrEmpty(id)
            && doc.Components?.Schemas != null
            && doc.Components.Schemas.TryGetValue(id, out var resolved))
        {
            return resolved;
        }

        return schema;
    }

    private static Dictionary<string, T> Index<T>(IEnumerable<T>? items, Func<T, string> key)
    {
        return (items ?? Enumerable.Empty<T>())
            .GroupBy(key)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static string RequestKey(RequestResponse request) => $"{request.Topic}@{request.Version}";

    private static string Describe(OpenApiSchema schema) =>
        string.IsNullOrEmpty(schema.Format) ? (schema.Type ?? "object") : $"{schema.Type}/{schema.Format}";

    private SchemaChange Change(SchemaChangeKind kind, SchemaDirection direction, string topic, string path, string description) =>
        new(kind, direction, topic, path, description, _rules.Evaluate(kind, direction));
}

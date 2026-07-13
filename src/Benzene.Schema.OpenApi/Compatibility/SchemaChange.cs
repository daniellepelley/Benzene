namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// A single detected difference between two versions of a service's schema, already classified for
/// compatibility.
/// </summary>
public class SchemaChange
{
    public SchemaChange(SchemaChangeKind kind, SchemaDirection direction, string topic, string path,
        string description, ChangeCompatibility compatibility)
    {
        Kind = kind;
        Direction = direction;
        Topic = topic;
        Path = path;
        Description = description;
        Compatibility = compatibility;
    }

    /// <summary>The kind of change.</summary>
    public SchemaChangeKind Kind { get; }

    /// <summary>Which side of the message the change is on.</summary>
    public SchemaDirection Direction { get; }

    /// <summary>The topic whose schema changed.</summary>
    public string Topic { get; }

    /// <summary>A dotted path to the changed element, e.g. <c>order:create.request.customerId</c>.</summary>
    public string Path { get; }

    /// <summary>A human-readable description of the change.</summary>
    public string Description { get; }

    /// <summary>How this change was classified by the rules in effect.</summary>
    public ChangeCompatibility Compatibility { get; }

    public override string ToString() => $"[{Compatibility}] {Path} ({Direction}): {Description}";
}

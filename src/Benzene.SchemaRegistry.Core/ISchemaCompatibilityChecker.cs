namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// Decides whether a candidate schema is compatible with the current latest for its subject. Pulled
/// out as a seam because a real structural check (does an Avro/JSON change preserve read/write
/// compatibility) is format-specific — a registry adapter usually delegates this to the server, and
/// an in-process registry can be given a smarter checker than the textual default.
/// </summary>
public interface ISchemaCompatibilityChecker
{
    /// <summary>
    /// Returns whether <paramref name="candidate"/> may be registered given the subject's current
    /// <paramref name="latest"/> version (which is <c>null</c> when the subject has no versions yet).
    /// </summary>
    /// <param name="latest">The subject's current latest version, or <c>null</c> if it's the first.</param>
    /// <param name="candidate">The schema being registered/checked.</param>
    /// <param name="mode">The compatibility level to enforce.</param>
    bool IsCompatible(RegisteredSchema? latest, SchemaDefinition candidate, SchemaCompatibilityMode mode);
}

/// <summary>
/// The default <see cref="ISchemaCompatibilityChecker"/>: a first schema for a subject is always
/// compatible, <see cref="SchemaCompatibilityMode.None"/> accepts anything, and otherwise the
/// candidate must be textually identical to the latest. This is deliberately conservative — it never
/// falsely approves a structural change; supply a format-aware checker (or rely on the registry
/// server's own check) for true evolution rules.
/// </summary>
public class TextualSchemaCompatibilityChecker : ISchemaCompatibilityChecker
{
    /// <inheritdoc />
    public bool IsCompatible(RegisteredSchema? latest, SchemaDefinition candidate, SchemaCompatibilityMode mode)
    {
        if (latest is null || mode == SchemaCompatibilityMode.None)
        {
            return true;
        }

        return string.Equals(latest.Schema, candidate.Schema, StringComparison.Ordinal);
    }
}

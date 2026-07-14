namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// Classifies each kind of schema change (per direction) as compatible, a warning, or breaking.
/// Ships with sensible defaults that encode standard producer/consumer compatibility semantics, and
/// lets you override any individual rule — this is the "user-configurable rules" layer that decides
/// what counts as a breaking change for your service.
/// </summary>
/// <example>
/// <code>
/// // Treat removing a request property as breaking rather than the default warning:
/// var rules = SchemaCompatibilityRules.Default()
///     .Set(SchemaChangeKind.PropertyRemoved, SchemaDirection.Request, ChangeCompatibility.Breaking);
/// </code>
/// </example>
public class SchemaCompatibilityRules
{
    private readonly Dictionary<(SchemaChangeKind, SchemaDirection), ChangeCompatibility> _overrides = new();

    /// <summary>Overrides the classification for a specific change kind and direction.</summary>
    /// <returns>This instance, for chaining.</returns>
    public SchemaCompatibilityRules Set(SchemaChangeKind kind, SchemaDirection direction, ChangeCompatibility compatibility)
    {
        _overrides[(kind, direction)] = compatibility;
        return this;
    }

    /// <summary>
    /// Classifies a change, using an override if one was set for this kind/direction, otherwise the default.
    /// </summary>
    public ChangeCompatibility Evaluate(SchemaChangeKind kind, SchemaDirection direction)
    {
        return _overrides.TryGetValue((kind, direction), out var value)
            ? value
            : DefaultFor(kind, direction);
    }

    /// <summary>The default rule set.</summary>
    public static SchemaCompatibilityRules Default() => new();

    /// <summary>
    /// A strict rule set: any change that isn't compatible by default is escalated to breaking (so
    /// warnings become breaking). Useful when you want zero tolerance for ambiguous changes.
    /// </summary>
    public static SchemaCompatibilityRules Strict()
    {
        var rules = new SchemaCompatibilityRules();
        foreach (SchemaChangeKind kind in Enum.GetValues(typeof(SchemaChangeKind)))
        {
            foreach (SchemaDirection direction in Enum.GetValues(typeof(SchemaDirection)))
            {
                if (DefaultFor(kind, direction) != ChangeCompatibility.Compatible)
                {
                    rules.Set(kind, direction, ChangeCompatibility.Breaking);
                }
            }
        }

        return rules;
    }

    /// <summary>
    /// The built-in default classification. Encodes that the client <em>produces</em> requests and
    /// <em>consumes</em> responses, so the same change is breaking on one side and safe on the other.
    /// </summary>
    public static ChangeCompatibility DefaultFor(SchemaChangeKind kind, SchemaDirection direction)
    {
        switch (kind)
        {
            case SchemaChangeKind.TopicAdded:
                return ChangeCompatibility.Compatible;

            case SchemaChangeKind.TopicRemoved:
                return ChangeCompatibility.Breaking;

            case SchemaChangeKind.TypeChanged:
                return ChangeCompatibility.Breaking;

            case SchemaChangeKind.PropertyAdded:
                // An optional field is safe: the client simply doesn't send it (request) or ignores it (response).
                return ChangeCompatibility.Compatible;

            case SchemaChangeKind.RequiredPropertyAdded:
                // A new required field breaks the producer of the object.
                return direction == SchemaDirection.Response
                    ? ChangeCompatibility.Compatible   // client tolerates an extra field it wasn't expecting
                    : ChangeCompatibility.Breaking;    // service now demands a field the client won't send

            case SchemaChangeKind.PropertyRemoved:
                // Removing a field breaks the consumer of the object.
                return direction == SchemaDirection.Response
                    ? ChangeCompatibility.Breaking     // client may read the removed response field
                    : ChangeCompatibility.Warning;     // service ignores a field the client still sends

            case SchemaChangeKind.PropertyBecameRequired:
                return direction == SchemaDirection.Response
                    ? ChangeCompatibility.Compatible
                    : ChangeCompatibility.Breaking;

            case SchemaChangeKind.PropertyBecameOptional:
                return direction == SchemaDirection.Response
                    ? ChangeCompatibility.Warning      // client may rely on the field always being present
                    : ChangeCompatibility.Compatible;

            default:
                return ChangeCompatibility.Warning;
        }
    }
}

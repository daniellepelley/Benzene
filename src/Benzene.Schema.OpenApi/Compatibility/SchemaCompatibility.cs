using Benzene.Schema.OpenApi.EventService;

namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// Entry point for contract compatibility checking. Given the schema a client was generated against
/// (the <em>baseline</em>) and the service's current schema, it reports whether the contract still
/// holds and, if not, exactly what changed and how severely.
/// </summary>
/// <remarks>
/// This is the semantic counterpart to the schema-hash contract check: a hash tells you the contract
/// changed; this tells you whether that change is breaking, and lets you tune what "breaking" means
/// via <see cref="SchemaCompatibilityRules"/>.
/// </remarks>
public static class SchemaCompatibility
{
    /// <summary>
    /// Compares a client's baseline schema against the service's current schema using the default rules.
    /// </summary>
    public static SchemaCompatibilityReport Compare(EventServiceDocument baseline, EventServiceDocument current) =>
        new SchemaCompatibilityComparer().Compare(baseline, current);

    /// <summary>
    /// Compares a client's baseline schema against the service's current schema using the given rules.
    /// </summary>
    public static SchemaCompatibilityReport Compare(EventServiceDocument baseline, EventServiceDocument current,
        SchemaCompatibilityRules rules) =>
        new SchemaCompatibilityComparer(rules).Compare(baseline, current);
}

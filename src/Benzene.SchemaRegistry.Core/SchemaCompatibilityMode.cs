namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// How a new schema version must relate to the existing ones for a subject — the standard
/// schema-registry compatibility levels.
/// </summary>
public enum SchemaCompatibilityMode
{
    /// <summary>No compatibility is enforced; any new schema is accepted.</summary>
    None,

    /// <summary>Consumers using the new schema can read data written with the previous schema.</summary>
    Backward,

    /// <summary>Consumers using the previous schema can read data written with the new schema.</summary>
    Forward,

    /// <summary>Both <see cref="Backward"/> and <see cref="Forward"/> hold.</summary>
    Full
}

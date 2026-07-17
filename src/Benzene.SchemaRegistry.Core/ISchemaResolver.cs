namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// Maps a CLR message type to the <see cref="SchemaDefinition"/> (subject + schema text + format) to
/// register for it. Kept as a seam so the schema source stays pluggable and format-specific — e.g. an
/// adapter over <c>Benzene.Avro</c>'s <c>IAvroSchemaResolver</c> supplies the Avro schema, without
/// this package depending on Avro.
/// </summary>
public interface ISchemaResolver
{
    /// <summary>Returns the schema definition to register for <paramref name="type"/>.</summary>
    SchemaDefinition Resolve(Type type);
}

/// <summary>An <see cref="ISchemaResolver"/> backed by an inline function.</summary>
public class DelegateSchemaResolver : ISchemaResolver
{
    private readonly Func<Type, SchemaDefinition> _resolve;

    /// <summary>Initializes the resolver from a function.</summary>
    public DelegateSchemaResolver(Func<Type, SchemaDefinition> resolve) => _resolve = resolve;

    /// <inheritdoc />
    public SchemaDefinition Resolve(Type type) => _resolve(type);
}

namespace Benzene.JsonSchema;

/// <summary>
/// A registry of hand-authored (bring-your-own) JSON Schemas keyed by the request CLR type they
/// validate. Populate at startup and register via
/// <see cref="DependencyInjectionExtensions.AddSuppliedJsonSchemas"/> so request validation runs
/// against the authored schema instead of one generated from the type. Feed it from the same
/// schema documents as <c>Benzene.Schema.OpenApi</c>'s <c>SuppliedSchemaCatalog</c> to keep the
/// published contract and runtime validation aligned.
/// </summary>
public class SuppliedJsonSchemaCatalog
{
    private readonly Dictionary<Type, Json.Schema.JsonSchema> _schemasByType = new();

    /// <summary>Looks up the supplied schema for a request type.</summary>
    /// <param name="type">The request type.</param>
    /// <param name="schema">The supplied schema when found.</param>
    /// <returns><c>true</c> when the type has a supplied schema.</returns>
    public bool TryGetSchema(Type type, out Json.Schema.JsonSchema schema)
    {
        return _schemasByType.TryGetValue(type, out schema!);
    }

    /// <summary>Adds a schema for a request type.</summary>
    /// <param name="type">The request type the schema validates.</param>
    /// <param name="schema">The schema.</param>
    /// <returns>The same catalog, for chaining.</returns>
    public SuppliedJsonSchemaCatalog Add(Type type, Json.Schema.JsonSchema schema)
    {
        _schemasByType[type] = schema;
        return this;
    }

    /// <summary>Adds a schema for a request type from its JSON text.</summary>
    /// <param name="type">The request type the schema validates.</param>
    /// <param name="schemaJson">The JSON Schema document as JSON text.</param>
    /// <returns>The same catalog, for chaining.</returns>
    public SuppliedJsonSchemaCatalog AddJson(Type type, string schemaJson)
    {
        return Add(type, Json.Schema.JsonSchema.FromText(schemaJson));
    }
}

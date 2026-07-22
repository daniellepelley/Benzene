using System.Text.Json;
using Benzene.Core.Exceptions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Benzene.Schema.OpenApi;

/// <summary>
/// A registry of hand-authored (bring-your-own) payload schemas: <c>schemaId → OpenApiSchema</c>
/// entries plus <c>Type → schemaId</c> mappings for the CLR types they describe. Populate it at
/// startup — programmatically, from per-schema JSON, or from a <c>components.schemas</c>-shaped
/// JSON object — then register it via <see cref="Extensions.AddSuppliedSchemas"/> so the spec
/// serves the authored schemas instead of reflecting them from the CLR types.
/// </summary>
/// <remarks>
/// Entries without a type mapping are legitimate: they are schemas that mapped entries
/// <c>$ref</c>erence. <see cref="SuppliedSchemaBuilder"/> registers the whole catalog into a
/// document's components on first use, so cross-references between supplied schemas resolve as
/// long as the referenced schema is in the catalog under the referenced id
/// (<c>#/components/schemas/&lt;schemaId&gt;</c>).
/// </remarks>
public class SuppliedSchemaCatalog
{
    private readonly Dictionary<string, OpenApiSchema> _schemasById = new();
    private readonly Dictionary<Type, string> _schemaIdsByType = new();
    private readonly OpenApiStringReader _reader = new();

    /// <summary>Every supplied schema, keyed by schema id.</summary>
    public IReadOnlyDictionary<string, OpenApiSchema> SchemasById => _schemasById;

    /// <summary>Looks up the schema id mapped to a CLR type.</summary>
    /// <param name="type">The payload type.</param>
    /// <param name="schemaId">The mapped schema id when found.</param>
    /// <returns><c>true</c> when the type has a supplied schema.</returns>
    public bool TryGetSchemaId(Type type, out string schemaId)
    {
        return _schemaIdsByType.TryGetValue(type, out schemaId!);
    }

    /// <summary>Adds a schema for a CLR type.</summary>
    /// <param name="type">The payload type the schema describes.</param>
    /// <param name="schemaId">The components key (and <c>$ref</c> target) for the schema.</param>
    /// <param name="schema">The schema.</param>
    /// <returns>The same catalog, for chaining.</returns>
    public SuppliedSchemaCatalog Add(Type type, string schemaId, OpenApiSchema schema)
    {
        Add(schemaId, schema);
        _schemaIdsByType[type] = schemaId;
        return this;
    }

    /// <summary>Adds a referenced-only schema (no CLR type mapping).</summary>
    /// <param name="schemaId">The components key (and <c>$ref</c> target) for the schema.</param>
    /// <param name="schema">The schema.</param>
    /// <returns>The same catalog, for chaining.</returns>
    public SuppliedSchemaCatalog Add(string schemaId, OpenApiSchema schema)
    {
        _schemasById[schemaId] = schema;
        return this;
    }

    /// <summary>Adds a schema for a CLR type from its JSON text (an OpenAPI 3.0 schema object).</summary>
    /// <param name="type">The payload type the schema describes.</param>
    /// <param name="schemaId">The components key (and <c>$ref</c> target) for the schema.</param>
    /// <param name="schemaJson">The schema document as JSON.</param>
    /// <returns>The same catalog, for chaining.</returns>
    public SuppliedSchemaCatalog AddJson(Type type, string schemaId, string schemaJson)
    {
        return Add(type, schemaId, ReadSchema(schemaId, schemaJson));
    }

    /// <summary>
    /// Adds every schema in a <c>components.schemas</c>-shaped JSON object (schema id → schema),
    /// mapping the listed CLR types to their schema ids. Unlisted entries are added as
    /// referenced-only schemas.
    /// </summary>
    /// <param name="componentsSchemasJson">A JSON object whose properties are schema ids and values are OpenAPI 3.0 schema objects.</param>
    /// <param name="typeMappings">The CLR type → schema id mappings for the entries that describe payload types.</param>
    /// <returns>The same catalog, for chaining.</returns>
    /// <exception cref="BenzeneException">Thrown when a mapped schema id is absent from the document.</exception>
    public SuppliedSchemaCatalog AddComponentsJson(string componentsSchemasJson,
        IReadOnlyDictionary<Type, string> typeMappings)
    {
        using var document = JsonDocument.Parse(componentsSchemasJson);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            Add(property.Name, ReadSchema(property.Name, property.Value.GetRawText()));
        }

        foreach (var mapping in typeMappings)
        {
            if (!_schemasById.ContainsKey(mapping.Value))
            {
                throw new BenzeneException(
                    $"Supplied schema document has no schema '{mapping.Value}' to map to type '{mapping.Key.FullName}'");
            }

            _schemaIdsByType[mapping.Key] = mapping.Value;
        }

        return this;
    }

    private OpenApiSchema ReadSchema(string schemaId, string schemaJson)
    {
        var schema = _reader.ReadFragment<OpenApiSchema>(schemaJson, OpenApiSpecVersion.OpenApi3_0,
            out var diagnostic);
        if (diagnostic.Errors.Any())
        {
            throw new BenzeneException(
                $"Supplied schema '{schemaId}' is not a valid OpenAPI 3.0 schema: {string.Join("; ", diagnostic.Errors)}");
        }

        return schema;
    }
}

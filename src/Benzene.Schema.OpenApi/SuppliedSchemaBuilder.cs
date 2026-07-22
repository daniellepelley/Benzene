using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi;

/// <summary>
/// An <see cref="ISchemaBuilder"/> that serves hand-authored schemas from a
/// <see cref="SuppliedSchemaCatalog"/> for the types it maps, and falls back to the wrapped
/// builder's reflection generation for every other type. Wire it via
/// <see cref="Extensions.AddSuppliedSchemas"/>; <see cref="SpecBuilder"/> picks it up through the
/// <see cref="ISchemaBuilder"/> DI seam.
/// </summary>
/// <remarks>
/// On the first mapped hit the entire catalog is registered into the document's components, so
/// <c>$ref</c>s between supplied schemas resolve. Like <see cref="SchemaBuilder"/>, an instance
/// accumulates one document's components — register transient/scoped, never singleton.
/// </remarks>
public class SuppliedSchemaBuilder : ISchemaBuilder
{
    private readonly SuppliedSchemaCatalog _catalog;
    private readonly ISchemaBuilder _fallback;
    private bool _catalogRegistered;

    /// <summary>Initializes the builder over a catalog and a fallback (typically <see cref="SchemaBuilder"/>).</summary>
    /// <param name="catalog">The supplied schemas.</param>
    /// <param name="fallback">The builder used for types the catalog doesn't map, and as the components store.</param>
    public SuppliedSchemaBuilder(SuppliedSchemaCatalog catalog, ISchemaBuilder fallback)
    {
        _catalog = catalog;
        _fallback = fallback;
    }

    /// <inheritdoc />
    public Dictionary<string, OpenApiSchema> Build()
    {
        return _fallback.Build();
    }

    /// <inheritdoc />
    public OpenApiSchema AddSchema(Type type)
    {
        if (!_catalog.TryGetSchemaId(type, out var schemaId))
        {
            return _fallback.AddSchema(type);
        }

        EnsureCatalogRegistered();
        return new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = schemaId,
                Type = ReferenceType.Schema,
            }
        };
    }

    /// <inheritdoc />
    public OpenApiSchema AddSchema(string schemaId, OpenApiSchema openApiSchema)
    {
        return _fallback.AddSchema(schemaId, openApiSchema);
    }

    private void EnsureCatalogRegistered()
    {
        if (_catalogRegistered)
        {
            return;
        }

        _catalogRegistered = true;
        foreach (var schema in _catalog.SchemasById)
        {
            _fallback.AddSchema(schema.Key, schema.Value);
        }
    }
}

using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.Examples;

/// <summary>
/// Default <see cref="ISchemaGetter"/> backed by a schema dictionary (typically the
/// <c>components.schemas</c> of a built spec document).
/// </summary>
public class SchemaGetter : ISchemaGetter
{
    private readonly IDictionary<string, OpenApiSchema> _schemas;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaGetter"/> class.
    /// </summary>
    /// <param name="schemas">The schema catalogue to resolve references against.</param>
    public SchemaGetter(IDictionary<string, OpenApiSchema> schemas)
    {
        _schemas = schemas;
    }

    /// <inheritdoc />
    public OpenApiSchema GetOpenApiSchema(OpenApiSchema openApiSchema)
    {
        return openApiSchema.Reference != null
            ? GetOpenApiSchema(openApiSchema.Reference.Id)
            : openApiSchema;
    }

    /// <inheritdoc />
    public OpenApiSchema GetOpenApiSchema(string id)
    {
        return _schemas[id];
    }
}

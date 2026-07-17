using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.Examples;

/// <summary>
/// Resolves OpenAPI schema references against a schema catalogue (typically a spec document's
/// <c>components.schemas</c>), so consumers can walk a schema tree without special-casing
/// <c>$ref</c> nodes.
/// </summary>
public interface ISchemaGetter
{
    /// <summary>
    /// Resolves a schema to its definition: a <c>$ref</c> schema is looked up in the catalogue,
    /// any other schema is returned as-is.
    /// </summary>
    /// <param name="openApiSchema">The schema (possibly a reference) to resolve.</param>
    /// <returns>The resolved schema.</returns>
    OpenApiSchema GetOpenApiSchema(OpenApiSchema openApiSchema);

    /// <summary>
    /// Gets a schema definition from the catalogue by its id.
    /// </summary>
    /// <param name="id">The schema id (the key in <c>components.schemas</c>).</param>
    /// <returns>The schema definition.</returns>
    OpenApiSchema GetOpenApiSchema(string id);
}

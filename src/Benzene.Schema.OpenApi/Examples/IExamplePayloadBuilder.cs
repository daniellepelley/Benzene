using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.Examples;

/// <summary>
/// Builds an example payload (as a property-name → value dictionary, ready for JSON serialization)
/// from an OpenAPI schema. Implementations are deterministic: the same schema always produces the
/// same example, so generated examples are stable across spec builds and golden-file tests.
/// </summary>
public interface IExamplePayloadBuilder
{
    /// <summary>
    /// Builds an example payload for the given object schema.
    /// </summary>
    /// <param name="openApiSchema">The schema (possibly a <c>$ref</c>) to build an example for.</param>
    /// <param name="schemaGetter">Resolves <c>$ref</c> schemas against the spec's schema catalogue.</param>
    /// <returns>The example payload as a dictionary keyed by camelCased property name.</returns>
    IDictionary<string, object> Build(OpenApiSchema openApiSchema, ISchemaGetter schemaGetter);
}

using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Benzene.Schema.OpenApi.Examples;

/// <summary>
/// Convenience extensions for <see cref="IExamplePayloadBuilder"/>.
/// </summary>
public static class ExamplePayloadBuilderExtensions
{
    /// <summary>
    /// Builds an example payload and serializes it to JSON.
    /// </summary>
    /// <param name="source">The example payload builder.</param>
    /// <param name="openApiSchema">The schema (possibly a <c>$ref</c>) to build an example for.</param>
    /// <param name="schemaGetter">Resolves <c>$ref</c> schemas against the spec's schema catalogue.</param>
    /// <returns>The example payload as a JSON string.</returns>
    public static string BuildAsJson(this IExamplePayloadBuilder source, OpenApiSchema openApiSchema,
        ISchemaGetter schemaGetter)
    {
        return JsonConvert.SerializeObject(source.Build(openApiSchema, schemaGetter));
    }

    /// <summary>
    /// Builds an example payload directly from a .NET type, generating its schema on the fly.
    /// </summary>
    /// <param name="source">The example payload builder.</param>
    /// <param name="type">The type to build an example payload for.</param>
    /// <returns>The example payload as a dictionary keyed by camelCased property name.</returns>
    public static IDictionary<string, object> Build(this IExamplePayloadBuilder source, Type type)
    {
        var schemaBuilder = new SchemaBuilder();
        var schema = schemaBuilder.AddSchema(type);
        return source.Build(schema, new SchemaGetter(schemaBuilder.Build()));
    }
}

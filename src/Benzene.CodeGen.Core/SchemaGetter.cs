using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Core;

public class SchemaGetter : ISchemaGetter
{
    private readonly IDictionary<string, OpenApiSchema> _schemas;

    public SchemaGetter(IDictionary<string, OpenApiSchema> schemas)
    {
        _schemas = schemas;
    }

    public OpenApiSchema GetOpenApiSchema(OpenApiSchema openApiSchema)
    {
        return openApiSchema.Reference != null
            ? GetOpenApiSchema(openApiSchema.Reference.Id)
            : openApiSchema;
    }

    public OpenApiSchema GetOpenApiSchema(string id)
    {
        return _schemas[id];
    }
}

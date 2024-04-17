using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Benzene.Schema.OpenApi;

public class SchemaBuilder : ISchemaBuilder 
{
    private readonly SchemaRepository _schemaRepository = new();

    public Dictionary<string, OpenApiSchema> Build()
    {
        return _schemaRepository.Schemas
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
    }
        
    public OpenApiSchema AddSchema(Type type)
    {
        var schemaGenerator = new SchemaGenerator(new SchemaGeneratorOptions(), 
            new JsonSerializerDataContractResolver(new System.Text.Json.JsonSerializerOptions()));
        return schemaGenerator.GenerateSchema(type, _schemaRepository);
    }

    public OpenApiSchema AddSchema(string schemaId, OpenApiSchema openApiSchema)
    {
        if (!_schemaRepository.Schemas.ContainsKey(schemaId))
        {
            _schemaRepository.AddDefinition(schemaId, openApiSchema);
        }

        return new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Id = schemaId,
                Type = ReferenceType.Schema,
            }
        };
    }
}

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Benzene.Schema.OpenApi;

public class SchemaBuilder : ISchemaBuilder
{
    private readonly SchemaRepository _schemaRepository = new();
    private readonly SchemaGenerator _schemaGenerator;

    /// <summary>Initializes a builder with default (flattening) schema generation.</summary>
    public SchemaBuilder() : this(null)
    {
    }

    /// <summary>
    /// Initializes a builder whose reflection-based generation honors the given
    /// <see cref="SchemaGenerationOptions"/> (inheritance via <c>allOf</c>, polymorphism via
    /// <c>oneOf</c> + discriminator). Null options keep the default flattening behavior.
    /// </summary>
    /// <param name="options">The generation options, or <c>null</c> for defaults.</param>
    public SchemaBuilder(SchemaGenerationOptions? options)
    {
        _schemaGenerator = new SchemaGenerator(CreateGeneratorOptions(options),
            new JsonSerializerDataContractResolver(new System.Text.Json.JsonSerializerOptions()));
    }

    public Dictionary<string, OpenApiSchema> Build()
    {
        return _schemaRepository.Schemas
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public OpenApiSchema AddSchema(Type type)
    {
        return _schemaGenerator.GenerateSchema(type, _schemaRepository);
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

    private static SchemaGeneratorOptions CreateGeneratorOptions(SchemaGenerationOptions? options)
    {
        var generatorOptions = new SchemaGeneratorOptions();
        if (options == null)
        {
            return generatorOptions;
        }

        generatorOptions.UseAllOfForInheritance = options.UseAllOfForInheritance;
        generatorOptions.UseOneOfForPolymorphism = options.UseOneOfForPolymorphism;
        generatorOptions.SubTypesSelector = type =>
            options.SubTypesResolver?.Invoke(type) ?? JsonPolymorphism.GetDerivedTypes(type);
        generatorOptions.DiscriminatorNameSelector = type =>
            (options.DiscriminatorNameResolver ?? JsonPolymorphism.GetDiscriminatorName)(type)!;
        generatorOptions.DiscriminatorValueSelector = type =>
            (options.DiscriminatorValueResolver ?? JsonPolymorphism.GetDiscriminatorValue)(type)!;
        return generatorOptions;
    }
}

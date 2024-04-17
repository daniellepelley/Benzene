using Benzene.Abstractions.Validation;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi;

public class OpenApiValidationSchemaBuilder : ISchemaBuilder
{
    private readonly ISchemaBuilder _schemaBuilder;
    private readonly IValidationSchemaBuilder _validationSchemaBuilder;
    private readonly Dictionary<string, Action<OpenApiSchema, IValidationSchema>> _mappings;

    public OpenApiValidationSchemaBuilder(ISchemaBuilder schemaBuilder, IValidationSchemaBuilder validationSchemaBuilder)
    {
        _validationSchemaBuilder = validationSchemaBuilder;
        _schemaBuilder = schemaBuilder;
        _mappings = CreateMappings();
    }

    public Dictionary<string, OpenApiSchema> Build()
    {
        return _schemaBuilder.Build();
    }

    public OpenApiSchema AddSchema(Type type)
    {
        var output = _schemaBuilder.AddSchema(type);
        var validationSchemas = _validationSchemaBuilder.GetValidationSchemas(type);

        var schemas = _schemaBuilder.Build();


        if (schemas.TryGetValue(type.Name, out var schema))
        {
            foreach (var validationSchema in validationSchemas)
            {
                var property = schema.Properties[validationSchema.Key];

                property.Description = string.Join(". ", validationSchema.Value.Select(x => x.Description));

                var notEmpty = validationSchema.Value.FirstOrDefault(x => x.Name == ValidationConstants.NotEmpty || x.Name == ValidationConstants.NotNull);
                if (notEmpty != null)
                {
                    schema.Required.Add(validationSchema.Key);
                }

                foreach (var mapping in _mappings)
                {
                    Map(property, validationSchema.Value, mapping.Key, mapping.Value);
                }
            }
        }

        return output;
    }

    private static Dictionary<string, Action<OpenApiSchema, IValidationSchema>> CreateMappings()
    {
        var mappings = new Dictionary<string, Action<OpenApiSchema, IValidationSchema>>();
        mappings.Add(ValidationConstants.MinLength,
            (apiSchema, validationSchema) => apiSchema.MinLength = ((IMinLengthValidationSchema)validationSchema).Min);
        mappings.Add(ValidationConstants.MaxLength,
            (apiSchema, validationSchema) => apiSchema.MaxLength = ((IMaxLengthValidationSchema)validationSchema).Max);
        mappings.Add(ValidationConstants.Regex,
            (apiSchema, validationSchema) => apiSchema.Pattern = ((IRegexValidationSchema)validationSchema).Expression);
        mappings.Add(ValidationConstants.IsOneOf,
            (apiSchema, validationSchema) =>
        {
            var options = ((IIsOneOfValidationSchema)validationSchema).Options;
            foreach (var option in options)
            {
                apiSchema.Enum.Add(new OpenApiString(option));
            }
        });
        mappings.Add(ValidationConstants.NotNull, (apiSchema, _) => apiSchema.Nullable = false);
        mappings.Add(ValidationConstants.NotEmpty, (apiSchema, _) => apiSchema.Nullable = false);
        mappings.Add(ValidationConstants.IsGuid, (apiSchema, _) => apiSchema.Format = "uuid");
        mappings.Add(ValidationConstants.Email, (apiSchema, _) => apiSchema.Format = "email");
        return mappings;
    }

    private static void Map(OpenApiSchema openApiSchema, IValidationSchema[] validationSchemas, string name,
        Action<OpenApiSchema, IValidationSchema> mapAction)
    {
        var validationSchema = validationSchemas.FirstOrDefault(x => x.Name == name);

        if (validationSchema != null)
        {
            mapAction(openApiSchema, validationSchema);
        }
    }

    public OpenApiSchema AddSchema(string schemaId, OpenApiSchema openApiSchema)
    {
        var schema = _schemaBuilder.AddSchema(schemaId, openApiSchema);
        return schema;
    }
}

using ByteBard.AsyncAPI.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.AsyncApi
{
    /// <summary>
    /// Maps the <c>Microsoft.OpenApi</c> schema model (produced by <see cref="SchemaBuilder"/>) onto
    /// ByteBard's AsyncAPI 3.0 JSON Schema model.
    /// </summary>
    public static class Mapper
    {
        private const string SchemaRefPrefix = "#/components/schemas/";

        public static AsyncApiJsonSchema? Map(OpenApiSchema? input)
        {
            if (input == null)
            {
                return null;
            }

            // A $ref to a component schema becomes a reference node (itself an AsyncApiJsonSchema).
            if (input.Reference != null)
            {
                return new AsyncApiJsonSchemaReference(SchemaRefPrefix + input.Reference.Id);
            }

            var schema = new AsyncApiJsonSchema
            {
                Type = MapType(input.Type),
                Items = Map(input.Items),
                Format = input.Format,
                Description = input.Description,
                MaxLength = input.MaxLength,
                MinLength = input.MinLength,
                Pattern = input.Pattern,
                Nullable = input.Nullable,
                Minimum = input.Minimum.HasValue ? (double)input.Minimum.Value : null,
                Maximum = input.Maximum.HasValue ? (double)input.Maximum.Value : null,
            };

            foreach (var property in input.Properties)
            {
                var mapped = Map(property.Value);
                if (mapped != null)
                {
                    schema.Properties[property.Key] = mapped;
                }
            }

            foreach (var required in input.Required)
            {
                schema.Required.Add(required);
            }

            foreach (var value in input.Enum)
            {
                schema.Enum.Add(MapAny(value));
            }

            // Composition keywords: carry polymorphism (oneOf + discriminator) and inheritance
            // (allOf base $ref) through instead of silently flattening them out of the AsyncAPI
            // document - they're emitted by SchemaBuilder when SchemaGenerationOptions enables
            // them, and by hand-authored (supplied) schemas.
            MapComposition(input.OneOf, schema.OneOf);
            MapComposition(input.AllOf, schema.AllOf);
            MapComposition(input.AnyOf, schema.AnyOf);

            if (input.AdditionalProperties != null)
            {
                schema.AdditionalProperties = Map(input.AdditionalProperties);
            }

            if (input.Discriminator?.PropertyName is { Length: > 0 } discriminatorProperty)
            {
                schema.Discriminator = discriminatorProperty;
            }

            return schema;
        }

        private static void MapComposition(IList<OpenApiSchema>? input, IList<AsyncApiJsonSchema> output)
        {
            if (input == null)
            {
                return;
            }

            foreach (var branch in input)
            {
                var mapped = Map(branch);
                if (mapped != null)
                {
                    output.Add(mapped);
                }
            }
        }

        public static AsyncApiAny MapAny(IOpenApiAny openApiAny)
        {
            return openApiAny switch
            {
                OpenApiInteger v => new AsyncApiAny(v.Value),
                OpenApiLong v => new AsyncApiAny(v.Value),
                OpenApiString v => new AsyncApiAny(v.Value),
                OpenApiBoolean v => new AsyncApiAny(v.Value),
                OpenApiFloat v => new AsyncApiAny(v.Value),
                OpenApiDouble v => new AsyncApiAny(v.Value),
                OpenApiByte v => new AsyncApiAny(v.Value),
                OpenApiDate v => new AsyncApiAny(v.Value),
                OpenApiDateTime v => new AsyncApiAny(v.Value),
                _ => new AsyncApiAny(openApiAny?.ToString())
            };
        }

        public static SchemaType? MapType(string? type)
        {
            return type switch
            {
                null or "" => null,
                "null" => SchemaType.Null,
                "boolean" => SchemaType.Boolean,
                "object" => SchemaType.Object,
                "array" => SchemaType.Array,
                "number" => SchemaType.Number,
                "string" => SchemaType.String,
                "integer" => SchemaType.Integer,
                _ => null
            };
        }
    }
}

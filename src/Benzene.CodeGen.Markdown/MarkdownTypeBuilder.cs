using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Markdown;

public class MarkdownTypeBuilder
{
    private readonly ISchemaGetter _schemaGetter;

    public MarkdownTypeBuilder(ISchemaGetter schemaGetter)
    {
        _schemaGetter = schemaGetter;
    }

    public void BuildType(string key, ILineWriter lineWriter)
    {
        BuildType(_schemaGetter.GetOpenApiSchema(key), lineWriter);
    }

    public void BuildType(OpenApiSchema openApiSchema, ILineWriter lineWriter)
    {
        openApiSchema = _schemaGetter.GetOpenApiSchema(openApiSchema);
        
        if (openApiSchema.Format == "uuid")
        {
            using (lineWriter.StartIndent())
            {
                lineWriter.WriteLine("Guid");
            }
        }
        else if (openApiSchema.Type == "array")
        {
            lineWriter.WriteLine("{");
            using (lineWriter.StartIndent())
            {
                GetLines(openApiSchema.Reference?.ReferenceV2, openApiSchema, lineWriter);
            }
            lineWriter.WriteLine("}[]");
        }
        else
        {
            if (openApiSchema.Properties.Any())
            {
                lineWriter.WriteLine("{");
                using (lineWriter.StartIndent())
                {
                    GetLines(openApiSchema.Reference?.ReferenceV2, openApiSchema, lineWriter);
                }

                lineWriter.WriteLine("}");
            }
            else
            {
                lineWriter.WriteLine("{}");
            }
        }
    }

    private void GetLines(string reference, OpenApiSchema openApiSchema, ILineWriter lineWriter)
    {
        foreach (var property in GetInnerType(openApiSchema).Properties)
        {
            MapProperty(property.Key, reference, property.Value, lineWriter);
        }
    }

    private void MapProperty(string name, string? reference, OpenApiSchema openApiSchema, ILineWriter lineWriter)
    {
        if (openApiSchema.Type == "object")
        {
            if (openApiSchema.Properties.Any())
            {
                lineWriter.WriteLine($"{CodeGenHelpers.Camelcase(name)}: {{");

                using (lineWriter.StartIndent())
                {
                    GetLines(reference, openApiSchema, lineWriter);
                }

                lineWriter.WriteLine("}");
            }
            else
            {
                lineWriter.WriteLine("{}");
            }
        }
        else if (openApiSchema.Type == "array" && (openApiSchema.Items.Reference != null || openApiSchema.Items.Type == "object"))
        {
            if (openApiSchema.Items.Reference != null || openApiSchema.Items.Reference.ReferenceV2 == reference)
            {
                lineWriter.WriteLine($"{CodeGenHelpers.Camelcase(name)}: {{...}}[]");
            }
            else
            {
                var innerSchema = _schemaGetter.GetOpenApiSchema(openApiSchema.Items);
                if (innerSchema.Properties.Any())
                {
                    lineWriter.WriteLine($"{CodeGenHelpers.Camelcase(name)}: {{");
        
                    using (lineWriter.StartIndent())
                    {
                        GetLines(reference, innerSchema, lineWriter);
                    }
        
                    lineWriter.WriteLine("}[]");
        
                }
                else
                {
                    lineWriter.WriteLine("{}[]");
                }
            }
        }
        else if (openApiSchema.Reference != null)
        {
            if (openApiSchema.Reference.ReferenceV2 == reference)
            {
                lineWriter.WriteLine($"{CodeGenHelpers.Camelcase(name)}: {{...}}");
            }
            else
            {
                MapProperty(name, openApiSchema.Reference.ReferenceV2, _schemaGetter.GetOpenApiSchema(openApiSchema),
                    lineWriter);
            }
        }
        else
        {
            lineWriter.WriteLine($"{CodeGenHelpers.Camelcase(name)}: {GetPropertyTypeName(openApiSchema)}");
        }
    }
    private OpenApiSchema GetInnerType(OpenApiSchema openApiSchema)
    {
        return openApiSchema.Type == "array"
            ? _schemaGetter.GetOpenApiSchema(openApiSchema.Items)
            : _schemaGetter.GetOpenApiSchema(openApiSchema);
    }

    private static bool IsNotValueType(Type type)
    {
        return !type.IsArray &&
               !type.IsValueType &&
               type.Name != "String" &&
               type.Name != "Datetime" &&
               type.Name != "Object";
    }

    private string GetPropertyTypeName(OpenApiSchema openApiSchema)
    {
        if (openApiSchema == null)
        {
            return "Void";
        }

        if (openApiSchema.Reference != null && !string.IsNullOrEmpty(openApiSchema.Reference.Id))
        {
            return openApiSchema.Reference.Id;
        }

        if (openApiSchema.Type == "array")
        {
            var type = GetPropertyTypeName(openApiSchema.Items);
            return $"{type}[]";
        }

        if (openApiSchema.Type == "string" && openApiSchema.Format == "date-time")
        {
            return "dateTime";
        }

        if (openApiSchema.Type == "string" && openApiSchema.Format == "uuid")
        {
            return "guid";
        }

        if (openApiSchema.Type == "object" && openApiSchema.AdditionalProperties?.Type == "string")
        {
            return "Dictionary<string, string>";
        }

        if (openApiSchema.Type == "integer")
        {
            return openApiSchema.Nullable ? "int?" : "int";
        }

        if (openApiSchema.Type == "number")
        {
            return openApiSchema.Nullable ? "double?" : "double";
        }

        if (openApiSchema.Type == "boolean")
        {
            return "bool";
        }

        return openApiSchema.Type;
    }
}

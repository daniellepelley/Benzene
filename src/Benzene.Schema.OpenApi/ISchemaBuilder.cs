using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi;

public interface ISchemaBuilder
{
    Dictionary<string, OpenApiSchema> Build();
    OpenApiSchema AddSchema(Type type);
    OpenApiSchema AddSchema(string schemaId, OpenApiSchema openApiSchema);
}


public class OpenApiSchemaComparer
{
    public string[] Compare(OpenApiSchema schema1, OpenApiSchema schema2)
    {
        var output = new List<string>();
        if (schema1.Type != schema2.Type)
        {
            output.Add($"Type mismatch: {schema1.Type} vs {schema2.Type}");
        }
        if (schema1.Format != schema2.Format)
        {
            output.Add($"Format mismatch: {schema1.Format} vs {schema2.Format}");
        }
        if (schema1.Properties.Count != schema2.Properties.Count)
        {
            output.Add($"Properties count mismatch: {schema1.Properties.Count} vs {schema2.Properties.Count}");
        }
        else
        {
            foreach (var property in schema1.Properties)
            {
                if (schema2.Properties.TryGetValue(property.Key, out var otherProperty))
                {
                    output.AddRange(Compare(property.Value, otherProperty));
                }
                else
                {
                    output.Add($"Property {property.Key} is missing in the second schema");
                }
            }
        }

        return output.ToArray();
    }
    
}

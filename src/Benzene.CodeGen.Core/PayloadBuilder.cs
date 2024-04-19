using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Core;

public class PayloadBuilder : IPayloadBuilder
{
    private readonly IDictionary<string, object> _knownValues;

    public PayloadBuilder()
    {
        _knownValues = new Dictionary<string, object>();
    }

    public PayloadBuilder(IDictionary<string, object> knownValues)
    {
        _knownValues = knownValues;
    }

    public IDictionary<string, object> Build(OpenApiSchema openApiSchema, ISchemaGetter schemaGetter)
    {
        return schemaGetter.GetOpenApiSchema(openApiSchema).Properties
            .ToDictionary(x => x.Key.Camelcase().ToString(), x => GetValue(x.Key.Camelcase().ToString(), null, schemaGetter.GetOpenApiSchema(x.Value), schemaGetter));
    }

    private object GetValue(string key, string? reference, OpenApiSchema openApiSchema, ISchemaGetter schemaGetter)
    {
        if (openApiSchema.Type == "object")
        {
            if (IsMatch(openApiSchema, reference))
            {
                return new object();
            }

            return schemaGetter.GetOpenApiSchema(openApiSchema).Properties
                .ToDictionary(x => x.Key.Camelcase().ToString(), x => GetValue(x.Key.Camelcase().ToString(), GetReference(x.Value, reference), x.Value, schemaGetter));
        }

        if (openApiSchema.Type == "string" && openApiSchema.Format == "date-time")
        {
            return GetValue(key, "2023-01-01T12:00:00.000Z");
        }

        if (openApiSchema.Type == "string" && openApiSchema.Format == "uuid")
        {
            return GetValue(key, "11111111-1111-1111-1111-111111111111");
        }

        if (openApiSchema.Type == "array")
        {
            if (IsMatch(openApiSchema.Items, reference))
            {
                return Array.Empty<object>();
            }

            return new[] { GetValue(key, GetReference(openApiSchema.Items, reference), schemaGetter.GetOpenApiSchema(openApiSchema.Items), schemaGetter) };
        }

        if (openApiSchema.Type == "integer")
        {
            return GetValue(key, 42);
        }

        if (openApiSchema.Type == "number")
        {
            return GetValue(key, 42.42);
        }

        if (openApiSchema.Type == "boolean")
        {
            return GetValue(key, true);
        }

        if (IsMatch(openApiSchema, reference))
        {
            return new object();
        }

        return GetValue(key, "value");
    }

    private static string? GetReference(OpenApiSchema openApiSchema, string? reference)
    {
        return openApiSchema?.Reference?.ReferenceV2 ?? reference;
    }

    private static bool IsMatch(OpenApiSchema openApiSchema, string? reference)
    {
        return openApiSchema?.Reference?.ReferenceV2 != null &&
               !string.IsNullOrEmpty(reference) &&
               openApiSchema?.Reference?.ReferenceV2 == reference;
    }

    private object GetValue(string key, object defaultValue)
    {
        return _knownValues.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }
}

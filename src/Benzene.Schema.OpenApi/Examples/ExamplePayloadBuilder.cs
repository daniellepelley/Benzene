using System.Text;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.Examples;

/// <summary>
/// Default <see cref="IExamplePayloadBuilder"/>: walks an OpenAPI schema and produces a
/// deterministic example payload that respects the schema's own metadata and validation
/// constraints, so a generated example passes the validation rules the spec advertises wherever
/// that is derivable.
/// </summary>
/// <remarks>
/// Value selection, in precedence order:
/// <list type="number">
/// <item><description>A known value supplied via the constructor, keyed by camelCased property
/// path (<c>order.customer.email</c>) or bare property name (<c>email</c>).</description></item>
/// <item><description>The schema's own <c>example</c>, then <c>default</c>, then the first
/// <c>enum</c> value.</description></item>
/// <item><description>A fixed value per type/format — strings honour <c>uuid</c>,
/// <c>date-time</c>, <c>date</c>, <c>email</c> and <c>uri</c> formats and are sized within
/// <c>minLength</c>/<c>maxLength</c>; numbers are clamped into <c>minimum</c>/<c>maximum</c>.
/// A <c>pattern</c> with no other hint is not honoured (no regex reverse-generation).</description></item>
/// </list>
/// Property keys are camelCased to match the wire format produced by Benzene's default JSON
/// serializer. Reference cycles terminate: a <c>$ref</c> already being expanded higher up the
/// same branch produces <c>{}</c> (or <c>[]</c> for arrays), and expansion stops at a fixed
/// maximum reference depth.
/// </remarks>
public class ExamplePayloadBuilder : IExamplePayloadBuilder
{
    private const int MaxReferenceDepth = 8;
    private const string DefaultString = "value";

    private readonly IDictionary<string, object> _knownValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExamplePayloadBuilder"/> class with no known values.
    /// </summary>
    public ExamplePayloadBuilder()
        : this(new Dictionary<string, object>())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExamplePayloadBuilder"/> class.
    /// </summary>
    /// <param name="knownValues">
    /// Values that override generation, keyed by camelCased property path
    /// (<c>order.customer.email</c>) or bare camelCased property name (<c>email</c>).
    /// A path key wins over a bare-name key.
    /// </param>
    public ExamplePayloadBuilder(IDictionary<string, object> knownValues)
    {
        _knownValues = knownValues;
    }

    /// <inheritdoc />
    public IDictionary<string, object> Build(OpenApiSchema openApiSchema, ISchemaGetter schemaGetter)
    {
        var ancestors = new List<string>();
        var reference = GetReferenceId(openApiSchema);
        if (reference != null)
        {
            ancestors.Add(reference);
        }

        return BuildObject(schemaGetter.GetOpenApiSchema(openApiSchema), schemaGetter, string.Empty, ancestors);
    }

    private IDictionary<string, object> BuildObject(OpenApiSchema objectSchema, ISchemaGetter schemaGetter,
        string path, List<string> ancestors)
    {
        var output = new Dictionary<string, object>();
        foreach (var property in objectSchema.Properties)
        {
            var key = CamelCase(property.Key);
            var propertyPath = path.Length == 0 ? key : $"{path}.{key}";
            output[key] = GetValue(key, propertyPath, property.Value, schemaGetter, ancestors)!;
        }

        return output;
    }

    private object? GetValue(string key, string path, OpenApiSchema schema, ISchemaGetter schemaGetter,
        List<string> ancestors)
    {
        if (TryGetKnownValue(key, path, out var knownValue))
        {
            return knownValue;
        }

        var reference = GetReferenceId(schema);
        if (reference != null && IsBlocked(reference, ancestors))
        {
            return new Dictionary<string, object>();
        }

        var resolved = schemaGetter.GetOpenApiSchema(schema);

        if (resolved.Example != null)
        {
            return OpenApiAnyConverter.ToPlainValue(resolved.Example);
        }

        if (resolved.Default != null)
        {
            return OpenApiAnyConverter.ToPlainValue(resolved.Default);
        }

        if (resolved.Enum is { Count: > 0 })
        {
            return OpenApiAnyConverter.ToPlainValue(resolved.Enum[0]);
        }

        if (resolved.Type == "array")
        {
            if (resolved.Items == null)
            {
                return Array.Empty<object>();
            }

            var itemReference = GetReferenceId(resolved.Items);
            if (itemReference != null && IsBlocked(itemReference, ancestors))
            {
                return Array.Empty<object>();
            }

            return new[] { GetValue(key, path, resolved.Items, schemaGetter, ancestors) };
        }

        if (resolved.Type == "object" || resolved.Properties.Count > 0)
        {
            var nextAncestors = reference == null ? ancestors : new List<string>(ancestors) { reference };
            return BuildObject(resolved, schemaGetter, path, nextAncestors);
        }

        return resolved.Type switch
        {
            "string" => GetStringValue(resolved),
            "integer" => ClampInteger(42, resolved),
            "number" => ClampNumber(42.42, resolved),
            "boolean" => true,
            _ => DefaultString
        };
    }

    private static object GetStringValue(OpenApiSchema schema)
    {
        return schema.Format switch
        {
            "date-time" => "2023-01-01T12:00:00.000Z",
            "uuid" => "11111111-1111-1111-1111-111111111111",
            "date" => "2023-01-01",
            "email" => "user@example.com",
            "uri" => "https://example.com",
            _ => SizeString(DefaultString, schema)
        };
    }

    private static string SizeString(string value, OpenApiSchema schema)
    {
        if (schema.MinLength.HasValue && value.Length < schema.MinLength.Value)
        {
            var builder = new StringBuilder(value);
            while (builder.Length < schema.MinLength.Value)
            {
                builder.Append(DefaultString);
            }

            value = builder.ToString(0, schema.MinLength.Value);
        }

        if (schema.MaxLength.HasValue && value.Length > schema.MaxLength.Value)
        {
            value = value.Substring(0, schema.MaxLength.Value);
        }

        return value;
    }

    private static long ClampInteger(long value, OpenApiSchema schema)
    {
        if (schema.Minimum.HasValue && value < schema.Minimum.Value)
        {
            value = (long)Math.Ceiling(schema.Minimum.Value);
        }

        if (schema.Maximum.HasValue && value > schema.Maximum.Value)
        {
            value = (long)Math.Floor(schema.Maximum.Value);
        }

        return value;
    }

    private static double ClampNumber(double value, OpenApiSchema schema)
    {
        if (schema.Minimum.HasValue && value < (double)schema.Minimum.Value)
        {
            value = (double)schema.Minimum.Value;
        }

        if (schema.Maximum.HasValue && value > (double)schema.Maximum.Value)
        {
            value = (double)schema.Maximum.Value;
        }

        return value;
    }

    private static bool IsBlocked(string reference, List<string> ancestors)
    {
        return ancestors.Contains(reference) || ancestors.Count >= MaxReferenceDepth;
    }

    private static string? GetReferenceId(OpenApiSchema? schema)
    {
        return schema?.Reference?.Id;
    }

    private bool TryGetKnownValue(string key, string path, out object? value)
    {
        if (_knownValues.TryGetValue(path, out value))
        {
            return true;
        }

        return _knownValues.TryGetValue(key, out value);
    }

    private static string CamelCase(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        // Must match the wire exactly: the generated example is documented as the shape a caller
        // sends, and the service deserializes with System.Text.Json's JsonNamingPolicy.CamelCase
        // (see Benzene.Core.MessageHandlers.Serialization.JsonSerializer). Hand-rolling it (lowercase
        // the whole leading run of capitals) diverged for acronym-prefixed names - "IPAddress" became
        // "ipaddress" instead of STJ's "ipAddress", so the example bound to null against its own service.
        return System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(source);
    }
}

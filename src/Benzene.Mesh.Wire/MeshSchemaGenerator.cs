using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Benzene.Mesh.Wire;

/// <summary>
/// Derives the JSON Schema (the 2020-12 subset of docs/specification/mesh.md §2.1) describing how
/// System.Text.Json marshals values of a CLR type. Runs once per topic at startup, inside
/// <see cref="MeshDescriptorFactory"/> - never on the message hot path.
///
/// The CLR reading of the §2.1 mapping:
/// <list type="bullet">
/// <item>string/char/Guid → "string"; bool → "boolean"; integral types → "integer";
/// float/double/decimal → "number"; enums → "string"-or-"integer" is converter-dependent, so → {}</item>
/// <item>DateTime/DateTimeOffset (marshal RFC 3339) → "string" with format "date-time";
/// byte[] (marshals base64) → "string"</item>
/// <item>JsonElement/JsonNode/JsonDocument/object (shape unknowable statically) → {} (unconstrained)</item>
/// <item>Nullable&lt;T&gt; → T's schema with "null" added to its type</item>
/// <item>arrays/IEnumerable&lt;T&gt; → "array" with "items"; string-keyed IDictionary&lt;string,T&gt; →
/// "object" with "additionalProperties"</item>
/// <item>other classes/structs → "object" with "properties" from public readable properties,
/// honoring <see cref="JsonPropertyNameAttribute"/>/<see cref="JsonIgnoreAttribute"/> and camelCase
/// naming. A property is listed in "required" (declaration order - determinism feeds the
/// descriptor hash) unless it is nullable-annotated (NRT for reference types,
/// Nullable&lt;T&gt; for value types) or marked ignore-when-null/default - the CLR reading of "the
/// marshaler may omit it"</item>
/// <item>recursion is cut at the cycle with {} - schemas stay self-contained, no $ref</item>
/// <item>anything else the marshaler can't serialize → {}</item>
/// </list>
///
/// Schema "properties" objects emit their keys in lexicographic order (the spec's canonical order
/// for maps, matching the reference implementation); "required" keeps declaration order.
/// </summary>
public static class MeshSchemaGenerator
{
    public static JsonObject Derive(Type type)
    {
        return SchemaFor(type, new List<Type>(), new NullabilityInfoContext());
    }

    private static JsonObject SchemaFor(Type type, List<Type> visiting, NullabilityInfoContext nullability)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return AddNull(SchemaFor(underlying, visiting, nullability));
        }

        if (type == typeof(string) || type == typeof(char) || type == typeof(Guid))
        {
            return new JsonObject { ["type"] = "string" };
        }
        if (type == typeof(bool))
        {
            return new JsonObject { ["type"] = "boolean" };
        }
        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
        {
            return new JsonObject { ["type"] = "integer" };
        }
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return new JsonObject { ["type"] = "number" };
        }
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return new JsonObject { ["type"] = "string", ["format"] = "date-time" };
        }
        if (type == typeof(byte[]))
        {
            return new JsonObject { ["type"] = "string" }; // System.Text.Json base64-encodes byte[]
        }
        if (type == typeof(object) || type == typeof(JsonElement) || type == typeof(JsonDocument) ||
            typeof(JsonNode).IsAssignableFrom(type) || type.IsEnum)
        {
            return new JsonObject(); // shape unknowable statically (enums are converter-dependent)
        }

        if (TryGetDictionaryValueType(type, out var valueType))
        {
            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = SchemaFor(valueType, visiting, nullability)
            };
        }

        if (TryGetEnumerableElementType(type, out var elementType))
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = SchemaFor(elementType, visiting, nullability)
            };
        }

        if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
        {
            if (visiting.Contains(type))
            {
                return new JsonObject(); // cycle: cut with an unconstrained schema
            }
            visiting.Add(type);
            try
            {
                return ObjectSchema(type, visiting, nullability);
            }
            finally
            {
                visiting.RemoveAt(visiting.Count - 1);
            }
        }

        return new JsonObject(); // not JSON-marshalable: unconstrained
    }

    private static JsonObject ObjectSchema(Type type, List<Type> visiting, NullabilityInfoContext nullability)
    {
        var properties = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        var required = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            var ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignore is { Condition: JsonIgnoreCondition.Always })
            {
                continue;
            }

            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                       ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (properties.ContainsKey(name))
            {
                continue; // name conflict: first seen wins
            }

            properties[name] = SchemaFor(property.PropertyType, visiting, nullability);

            var optional =
                ignore is { Condition: JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault } ||
                Nullable.GetUnderlyingType(property.PropertyType) != null ||
                (!property.PropertyType.IsValueType &&
                 nullability.Create(property).WriteState == NullabilityState.Nullable);
            if (!optional)
            {
                required.Add(name);
            }
        }

        var propertiesNode = new JsonObject();
        foreach (var pair in properties)
        {
            propertiesNode[pair.Key] = pair.Value;
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = propertiesNode };
        if (required.Count > 0)
        {
            schema["required"] = new JsonArray(required.Select(name => (JsonNode)name).ToArray());
        }
        return schema;
    }

    /// <summary>Adds "null" to a schema's type, for Nullable&lt;T&gt;. An unconstrained {} passes through.</summary>
    private static JsonObject AddNull(JsonObject schema)
    {
        if (schema["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var single))
        {
            schema["type"] = new JsonArray(single, "null");
        }
        return schema;
    }

    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        var dictionary = new[] { type }.Concat(type.GetInterfaces())
            .FirstOrDefault(x => x.IsGenericType &&
                                 (x.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                                  x.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)) &&
                                 x.GetGenericArguments()[0] == typeof(string));
        valueType = dictionary?.GetGenericArguments()[1] ?? typeof(object);
        return dictionary != null;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }
        if (type == typeof(string))
        {
            elementType = typeof(object);
            return false;
        }
        var enumerable = new[] { type }.Concat(type.GetInterfaces())
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        elementType = enumerable?.GetGenericArguments()[0] ?? typeof(object);
        return enumerable != null;
    }
}

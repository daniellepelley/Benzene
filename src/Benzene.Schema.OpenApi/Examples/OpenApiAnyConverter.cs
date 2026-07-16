using Microsoft.OpenApi.Any;

namespace Benzene.Schema.OpenApi.Examples;

/// <summary>
/// Converts between plain .NET values (the <see cref="IExamplePayloadBuilder"/> output shape —
/// dictionaries, lists, and primitives) and the <see cref="IOpenApiAny"/> tree the OpenAPI
/// writers serialize. Used to honour schema <c>example</c>/<c>default</c>/<c>enum</c> values
/// during example generation, and to embed generated examples into spec documents.
/// </summary>
public static class OpenApiAnyConverter
{
    /// <summary>
    /// Converts an <see cref="IOpenApiAny"/> value to a plain .NET value: primitives become CLR
    /// primitives, objects become <c>IDictionary&lt;string, object&gt;</c>, arrays become lists.
    /// </summary>
    /// <param name="any">The OpenAPI value to convert.</param>
    /// <returns>The plain .NET value, or null for an OpenAPI null.</returns>
    public static object? ToPlainValue(IOpenApiAny any)
    {
        return any switch
        {
            OpenApiString s => s.Value,
            OpenApiInteger i => i.Value,
            OpenApiLong l => l.Value,
            OpenApiFloat f => f.Value,
            OpenApiDouble d => d.Value,
            OpenApiBoolean b => b.Value,
            OpenApiDateTime dateTime => dateTime.Value,
            OpenApiDate date => date.Value,
            OpenApiNull => null,
            OpenApiObject o => o.ToDictionary(x => x.Key, x => ToPlainValue(x.Value)!),
            OpenApiArray a => a.Select(ToPlainValue).ToList(),
            _ => any.ToString()
        };
    }

    /// <summary>
    /// Converts a plain .NET value (dictionary/list/primitive) to an <see cref="IOpenApiAny"/>
    /// tree, ready to be written into a spec document.
    /// </summary>
    /// <param name="value">The plain .NET value to convert.</param>
    /// <returns>The equivalent OpenAPI value.</returns>
    public static IOpenApiAny ToOpenApiAny(object? value)
    {
        return value switch
        {
            null => new OpenApiNull(),
            IOpenApiAny any => any,
            string s => new OpenApiString(s),
            bool b => new OpenApiBoolean(b),
            int i => new OpenApiInteger(i),
            long l => new OpenApiLong(l),
            float f => new OpenApiFloat(f),
            double d => new OpenApiDouble(d),
            decimal m => new OpenApiDouble((double)m),
            DateTimeOffset dto => new OpenApiDateTime(dto),
            DateTime dt => new OpenApiDateTime(dt),
            Guid g => new OpenApiString(g.ToString()),
            IDictionary<string, object> dictionary => ToOpenApiObject(dictionary),
            System.Collections.IEnumerable sequence => ToOpenApiArray(sequence),
            _ => new OpenApiString(value.ToString() ?? string.Empty)
        };
    }

    private static OpenApiObject ToOpenApiObject(IDictionary<string, object> dictionary)
    {
        var output = new OpenApiObject();
        foreach (var item in dictionary)
        {
            output[item.Key] = ToOpenApiAny(item.Value);
        }

        return output;
    }

    private static OpenApiArray ToOpenApiArray(System.Collections.IEnumerable sequence)
    {
        var output = new OpenApiArray();
        foreach (var item in sequence)
        {
            output.Add(ToOpenApiAny(item));
        }

        return output;
    }
}

using System.Text.Json;

namespace Benzene.Conformance.Test;

/// <summary>
/// Loads the language-neutral fixture files from docs/specification/conformance/ (copied to the test
/// output directory at build time) and provides the subset-matching JSON comparison the fixture format
/// specifies.
/// </summary>
public static class ConformanceFixtures
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T Load<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "conformance", fileName);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
               ?? throw new InvalidOperationException($"Fixture {fileName} deserialized to null.");
    }

    /// <summary>
    /// Asserts <paramref name="expected"/> is a subset of <paramref name="actual"/>: every property in an
    /// expected object must exist in the actual value and match recursively; arrays must match exactly in
    /// length and order; primitives must be equal. Extra properties in actual objects are ignored.
    /// Returns null on success, or a description of the first mismatch.
    /// </summary>
    public static string? FindSubsetMismatch(JsonElement expected, JsonElement actual, string path = "$")
    {
        if (expected.ValueKind == JsonValueKind.Object)
        {
            if (actual.ValueKind != JsonValueKind.Object)
            {
                return $"{path}: expected an object but found {actual.ValueKind}";
            }

            foreach (var property in expected.EnumerateObject())
            {
                if (!actual.TryGetProperty(property.Name, out var actualValue))
                {
                    return $"{path}.{property.Name}: missing";
                }

                var mismatch = FindSubsetMismatch(property.Value, actualValue, $"{path}.{property.Name}");
                if (mismatch != null)
                {
                    return mismatch;
                }
            }

            return null;
        }

        if (expected.ValueKind == JsonValueKind.Array)
        {
            if (actual.ValueKind != JsonValueKind.Array)
            {
                return $"{path}: expected an array but found {actual.ValueKind}";
            }

            var expectedItems = expected.EnumerateArray().ToArray();
            var actualItems = actual.EnumerateArray().ToArray();
            if (expectedItems.Length != actualItems.Length)
            {
                return $"{path}: expected {expectedItems.Length} items but found {actualItems.Length}";
            }

            for (var i = 0; i < expectedItems.Length; i++)
            {
                var mismatch = FindSubsetMismatch(expectedItems[i], actualItems[i], $"{path}[{i}]");
                if (mismatch != null)
                {
                    return mismatch;
                }
            }

            return null;
        }

        if (expected.ValueKind != actual.ValueKind)
        {
            return $"{path}: expected {expected.ValueKind} '{expected.GetRawText()}' but found {actual.ValueKind} '{actual.GetRawText()}'";
        }

        var matches = expected.ValueKind == JsonValueKind.String
            ? expected.GetString() == actual.GetString()
            : expected.GetRawText() == actual.GetRawText();

        return matches
            ? null
            : $"{path}: expected '{expected.GetRawText()}' but found '{actual.GetRawText()}'";
    }
}

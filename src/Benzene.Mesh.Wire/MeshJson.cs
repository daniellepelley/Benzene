using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benzene.Mesh.Wire;

/// <summary>
/// The one place this package's wire serialization settings live: camelCase names (the wire
/// contract's casing, docs/specification/wire-contracts.md §6), nulls omitted where a shape marks
/// them ignorable. Both the descriptor hash canonicalization and the trace exporter use these
/// options, so the two can't drift apart.
/// </summary>
public static class MeshJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}

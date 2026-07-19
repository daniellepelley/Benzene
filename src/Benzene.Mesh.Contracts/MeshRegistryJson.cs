using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benzene.Mesh.Contracts;

/// <summary>
/// Serializes/deserializes a <see cref="MeshServiceRegistry"/> to the <c>mesh.json</c>
/// <c>{ "services": [ { name, specUrl, healthUrl, source, sourceOptions, owningTeam } ] }</c> shape the aggregator
/// host reads. This is the concrete seam between the discovery phase (which <em>writes</em> this
/// document) and runtime monitoring (which <em>reads</em> it) — the generated document is a drop-in
/// for a hand-written <c>mesh.json</c>, so nothing in the aggregator changes.
/// </summary>
public static class MeshRegistryJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>Serializes the registry to the <c>mesh.json</c> services-document JSON.</summary>
    public static string Serialize(MeshServiceRegistry registry)
    {
        var dto = new RegistryDto
        {
            Services = registry.Services.Select(RegistryEntryDto.From).ToArray()
        };
        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>Deserializes a <c>mesh.json</c> services-document back into a registry.</summary>
    public static MeshServiceRegistry Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<RegistryDto>(json, Options);
        var services = (dto?.Services ?? Array.Empty<RegistryEntryDto>()).Select(e => e.ToEntry()).ToArray();
        return new MeshServiceRegistry(services);
    }

    private sealed class RegistryDto
    {
        public RegistryEntryDto[] Services { get; set; } = Array.Empty<RegistryEntryDto>();
    }

    private sealed class RegistryEntryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? SpecUrl { get; set; }
        public string? HealthUrl { get; set; }
        public string Source { get; set; } = MeshServiceSource.Http;
        public Dictionary<string, string>? SourceOptions { get; set; }
        public string? OwningTeam { get; set; }

        public static RegistryEntryDto From(MeshServiceRegistryEntry entry) => new()
        {
            Name = entry.Name,
            SpecUrl = string.IsNullOrEmpty(entry.SpecUrl) ? null : entry.SpecUrl,
            HealthUrl = string.IsNullOrEmpty(entry.HealthUrl) ? null : entry.HealthUrl,
            Source = entry.Source,
            SourceOptions = entry.SourceOptions?.ToDictionary(kv => kv.Key, kv => kv.Value),
            OwningTeam = entry.OwningTeam
        };

        public MeshServiceRegistryEntry ToEntry()
            => new(Name, SpecUrl ?? string.Empty, HealthUrl ?? string.Empty, Source, SourceOptions, OwningTeam);
    }
}

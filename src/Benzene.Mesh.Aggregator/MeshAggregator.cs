using System.Text.Json;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Polls every service in a <see cref="MeshServiceRegistry"/> for its spec and health documents,
/// computes contract-drift, and publishes the resulting catalog to an <see cref="IMeshArtifactStore"/>.
/// </summary>
public class MeshAggregator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IMeshArtifactStore _store;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>Initializes a new instance of the <see cref="MeshAggregator"/> class.</summary>
    /// <param name="httpClient">The client used to fetch each service's spec and health documents.</param>
    /// <param name="store">Where generated catalog artifacts are published (and, for contract-drift comparison, read back from).</param>
    /// <param name="clock">Supplies the current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Overridable for deterministic tests.</param>
    public MeshAggregator(HttpClient httpClient, IMeshArtifactStore store, Func<DateTimeOffset>? clock = null)
    {
        _httpClient = httpClient;
        _store = store;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Polls every registered service once, publishing a <c>services/{name}.json</c> snapshot per
    /// service and a top-level <c>manifest.json</c> summarizing all of them. A single service's
    /// spec/health fetch failing does not prevent the rest from being processed and published.
    /// </summary>
    /// <param name="registry">The services to poll.</param>
    /// <returns>The published manifest.</returns>
    public async Task<MeshManifest> RunOnceAsync(MeshServiceRegistry registry)
    {
        var manifestEntries = new List<MeshManifestEntry>();

        foreach (var entry in registry.Services)
        {
            var snapshot = await BuildSnapshotAsync(entry);
            await _store.PublishAsync($"services/{entry.Name}.json", JsonSerializer.Serialize(snapshot, JsonOptions));

            manifestEntries.Add(new MeshManifestEntry(
                entry.Name, DetermineStatus(snapshot), snapshot.ContractDrift, entry.SpecUrl, entry.HealthUrl));
        }

        var manifest = new MeshManifest(_clock(), manifestEntries.ToArray());
        await _store.PublishAsync("manifest.json", JsonSerializer.Serialize(manifest, JsonOptions));
        return manifest;
    }

    private async Task<MeshServiceSnapshot> BuildSnapshotAsync(MeshServiceRegistryEntry entry)
    {
        string? specJson = null;
        string? specHash = null;
        string? error = null;

        try
        {
            specJson = await _httpClient.GetStringAsync(entry.SpecUrl);
            specHash = MeshHashing.ComputeHash(specJson);
        }
        catch (Exception ex)
        {
            // Type name only, never the message - this artifact aggregates across services into
            // something with broader visibility than one service's own health endpoint (same
            // posture as the Data["Error"] fix across the HealthChecks family).
            error = ex.GetType().Name;
        }

        HealthCheckResponse? health = null;
        try
        {
            var healthJson = await _httpClient.GetStringAsync(entry.HealthUrl);
            health = JsonSerializer.Deserialize<HealthCheckResponse>(healthJson, JsonOptions);
        }
        catch (Exception ex)
        {
            error ??= ex.GetType().Name;
        }

        var previousSpecHash = await TryGetPreviousSpecHashAsync(entry.Name);
        var contractDrift = specHash != null && previousSpecHash != null && previousSpecHash != specHash;

        return new MeshServiceSnapshot(entry.Name, _clock(), specJson, specHash, previousSpecHash, contractDrift, health, error);
    }

    private async Task<string?> TryGetPreviousSpecHashAsync(string serviceName)
    {
        var previousJson = await _store.TryReadAsync($"services/{serviceName}.json");
        if (previousJson == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MeshServiceSnapshot>(previousJson, JsonOptions)?.SpecHash;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// A service's <see cref="MeshServiceSnapshot.Health"/> being unreachable/undeserializable is
    /// treated as <see cref="MeshServiceStatus.Unreachable"/> regardless of whether its spec endpoint
    /// happened to respond - health is the primary "is this service okay" signal, so not having one
    /// at all is the more important fact to surface than a spec fetch succeeding in isolation.
    /// </summary>
    private static string DetermineStatus(MeshServiceSnapshot snapshot)
    {
        if (snapshot.Health == null)
        {
            return MeshServiceStatus.Unreachable;
        }

        return snapshot.Health.IsHealthy ? MeshServiceStatus.Healthy : MeshServiceStatus.Unhealthy;
    }
}

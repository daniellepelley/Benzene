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

    // Matches Benzene.HealthChecks.TimeOutHealthCheck's 10-second convention: an explicit,
    // documented bound on each fetch rather than relying solely on a source's own (potentially much
    // longer) defaults - one slow/hung service shouldn't be able to stall a run.
    private static readonly TimeSpan PerServiceFetchTimeout = TimeSpan.FromSeconds(10);

    private readonly IReadOnlyDictionary<string, IMeshServiceSource> _sources;
    private readonly IMeshArtifactStore _store;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>Initializes a new instance of the <see cref="MeshAggregator"/> class.</summary>
    /// <param name="sources">
    /// Every registered <see cref="IMeshServiceSource"/>, keyed by <see cref="IMeshServiceSource.Key"/>
    /// (case-insensitive) to resolve each entry's <see cref="MeshServiceRegistryEntry.Source"/>
    /// against. An entry whose <c>Source</c> has no matching source here is recorded as that
    /// service's own fetch error, not a run-wide failure.
    /// </param>
    /// <param name="store">Where generated catalog artifacts are published (and, for contract-drift comparison, read back from).</param>
    /// <param name="clock">Supplies the current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Overridable for deterministic tests.</param>
    public MeshAggregator(IEnumerable<IMeshServiceSource> sources, IMeshArtifactStore store, Func<DateTimeOffset>? clock = null)
    {
        _sources = sources.ToDictionary(source => source.Key, StringComparer.OrdinalIgnoreCase);
        _store = store;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Polls every registered service once, publishing a <c>services/{name}.json</c> snapshot per
    /// service and a top-level <c>manifest.json</c> summarizing all of them. A single service's
    /// spec/health fetch failing (or timing out - see <c>PerServiceFetchTimeout</c>) does not
    /// prevent the rest from being processed and published. Services are polled concurrently, not
    /// one at a time, so one slow service adds to the run's total time only up to its own timeout,
    /// not to every other service's fetch time as well - the same shape as
    /// <c>Benzene.HealthChecks.HealthCheckProcessor.PerformHealthChecksAsync</c>.
    /// </summary>
    /// <param name="registry">The services to poll.</param>
    /// <returns>The published manifest.</returns>
    public async Task<MeshManifest> RunOnceAsync(MeshServiceRegistry registry)
    {
        var entries = registry.Services;
        var snapshots = await Task.WhenAll(entries.Select(BuildSnapshotAsync));

        var manifestEntries = new List<MeshManifestEntry>(entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var snapshot = snapshots[i];
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
        var source = ResolveSource(entry.Source);

        string? specJson = null;
        string? error = null;

        try
        {
            using var specTimeout = new CancellationTokenSource(PerServiceFetchTimeout);
            specJson = await source.FetchSpecAsync(entry, specTimeout.Token);
        }
        catch (Exception ex)
        {
            // Type name only, never the message - this artifact aggregates across services into
            // something with broader visibility than one service's own health endpoint (same
            // posture as the Data["Error"] fix across the HealthChecks family). A timeout surfaces
            // here as TaskCanceledException, same as any other fetch failure.
            error = ex.GetType().Name;
        }

        HealthCheckResponse? health = null;
        try
        {
            using var healthTimeout = new CancellationTokenSource(PerServiceFetchTimeout);
            var healthJson = await source.FetchHealthAsync(entry, healthTimeout.Token);
            health = JsonSerializer.Deserialize<HealthCheckResponse>(healthJson, JsonOptions);
        }
        catch (Exception ex)
        {
            error ??= ex.GetType().Name;
        }

        return await MeshSnapshotBuilder.BuildAsync(_store, entry.Name, _clock(), specJson, health, error);
    }

    private IMeshServiceSource ResolveSource(string sourceKey)
    {
        return _sources.TryGetValue(sourceKey, out var source) ? source : new UnknownMeshServiceSource(sourceKey);
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

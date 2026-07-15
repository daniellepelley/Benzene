using System.Text.Json;
using System.Threading;
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
    // documented bound on each fetch rather than relying solely on the injected HttpClient's own
    // (much longer, 100s-default) Timeout - one slow/hung service shouldn't be able to stall a run.
    private static readonly TimeSpan PerServiceFetchTimeout = TimeSpan.FromSeconds(10);

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
        string? specJson = null;
        string? specHash = null;
        string? error = null;

        try
        {
            using var specTimeout = new CancellationTokenSource(PerServiceFetchTimeout);
            specJson = await _httpClient.GetStringAsync(entry.SpecUrl, specTimeout.Token);
            specHash = MeshHashing.ComputeHash(specJson);
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
            // Deliberately not GetStringAsync: Benzene's own health-check middleware maps an
            // unhealthy aggregate result to HTTP 503 (see Benzene.HealthChecks.HealthCheckProcessor),
            // which GetStringAsync would treat as a fetch failure indistinguishable from the
            // service being genuinely unreachable. Reading the body regardless of status code lets
            // a real 503-with-a-valid-unhealthy-body come through as Unhealthy instead of
            // Unreachable - only a connection-level failure or an unparseable body should count as
            // unreachable.
            using var healthTimeout = new CancellationTokenSource(PerServiceFetchTimeout);
            using var response = await _httpClient.GetAsync(entry.HealthUrl, healthTimeout.Token);
            var healthJson = await response.Content.ReadAsStringAsync(healthTimeout.Token);
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

using System.Text.Json;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Builds a <see cref="MeshServiceSnapshot"/> from a spec/health fetch (or self-reported push),
/// computing the spec hash and comparing it against the previous run's via <see cref="IMeshArtifactStore"/>.
/// Shared by <see cref="MeshAggregator"/> (pulled snapshots) and any <c>IMeshReportPublisher</c> that
/// turns a pushed <c>MeshServiceReport</c> into the same snapshot shape, so both compute
/// <see cref="MeshServiceSnapshot.ContractDrift"/> identically instead of two copies of the same logic.
/// </summary>
internal static class MeshSnapshotBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Builds a snapshot, reading the previous run's spec hash from <paramref name="store"/> for drift comparison.</summary>
    /// <param name="store">Where the previous run's snapshot (if any) is read back from.</param>
    /// <param name="name">The service's name.</param>
    /// <param name="fetchedAtUtc">When this spec/health was obtained.</param>
    /// <param name="specJson">The service's spec document, verbatim, or <c>null</c> if it couldn't be obtained.</param>
    /// <param name="health">The service's health check response, or <c>null</c> if it couldn't be obtained.</param>
    /// <param name="error">The exception type name from a failed fetch, or <c>null</c>.</param>
    /// <returns>The built snapshot.</returns>
    public static async Task<MeshServiceSnapshot> BuildAsync(
        IMeshArtifactStore store, string name, DateTimeOffset fetchedAtUtc, string? specJson, HealthCheckResponse? health, string? error)
    {
        var specHash = specJson != null ? MeshHashing.ComputeHash(specJson) : null;
        var previousSpecHash = await TryGetPreviousSpecHashAsync(store, name);
        var contractDrift = specHash != null && previousSpecHash != null && previousSpecHash != specHash;

        return new MeshServiceSnapshot(name, fetchedAtUtc, specJson, specHash, previousSpecHash, contractDrift, health, error);
    }

    private static async Task<string?> TryGetPreviousSpecHashAsync(IMeshArtifactStore store, string serviceName)
    {
        var previousJson = await store.TryReadAsync($"services/{serviceName}.json");
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
}

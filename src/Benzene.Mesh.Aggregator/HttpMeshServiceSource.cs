using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// The default <see cref="IMeshServiceSource"/> - fetches <see cref="MeshServiceRegistryEntry.SpecUrl"/>/
/// <see cref="MeshServiceRegistryEntry.HealthUrl"/> over HTTP. This is <see cref="MeshAggregator"/>'s
/// original (pre-<see cref="IMeshServiceSource"/>) behavior, moved here unchanged.
/// </summary>
public class HttpMeshServiceSource : IMeshServiceSource
{
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="HttpMeshServiceSource"/> class.</summary>
    /// <param name="httpClient">The client used to fetch each service's spec and health documents.</param>
    public HttpMeshServiceSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public string Key => MeshServiceSource.Http;

    /// <inheritdoc />
    public Task<string> FetchSpecAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken)
    {
        return _httpClient.GetStringAsync(entry.SpecUrl, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> FetchHealthAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken)
    {
        // Deliberately not GetStringAsync: Benzene's own health-check middleware maps an unhealthy
        // aggregate result to HTTP 503 (see Benzene.HealthChecks.HealthCheckProcessor), which
        // GetStringAsync would treat as a fetch failure indistinguishable from the service being
        // genuinely unreachable. Reading the body regardless of status code lets a real
        // 503-with-a-valid-unhealthy-body come through as Unhealthy instead of Unreachable - only a
        // connection-level failure counts as unreachable.
        using var response = await _httpClient.GetAsync(entry.HealthUrl, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

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
    public async Task<string?> TryFetchSpecAsync(MeshServiceRegistryEntry entry, string specType, CancellationToken cancellationToken)
    {
        var url = SpecUrlForType(entry.SpecUrl, specType);
        return url == null ? null : await _httpClient.GetStringAsync(url, cancellationToken);
    }

    /// <summary>
    /// Derives an alternate-spec-type URL from the benzene <see cref="MeshServiceRegistryEntry.SpecUrl"/>
    /// by setting <c>type=&lt;specType&gt;&amp;format=json</c> and preserving the rest of the URL — the
    /// benzene and asyncapi specs are the same <c>spec</c> endpoint differing only by <c>type</c>.
    /// Returns <c>null</c> for a blank spec URL.
    /// </summary>
    internal static string? SpecUrlForType(string specUrl, string specType)
    {
        if (string.IsNullOrWhiteSpace(specUrl))
        {
            return null;
        }

        var fragmentIndex = specUrl.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? specUrl.Substring(fragmentIndex) : string.Empty;
        var withoutFragment = fragmentIndex >= 0 ? specUrl.Substring(0, fragmentIndex) : specUrl;

        var queryIndex = withoutFragment.IndexOf('?');
        var basePart = queryIndex >= 0 ? withoutFragment.Substring(0, queryIndex) : withoutFragment;
        var query = queryIndex >= 0 ? withoutFragment.Substring(queryIndex + 1) : string.Empty;

        // Keep any pre-existing query params except type/format, which we set ourselves.
        var pairs = query
            .Split('&')
            .Where(pair => pair.Length > 0
                           && !pair.StartsWith("type=", StringComparison.OrdinalIgnoreCase)
                           && !pair.StartsWith("format=", StringComparison.OrdinalIgnoreCase))
            .ToList();
        pairs.Insert(0, "format=json");
        pairs.Insert(0, "type=" + Uri.EscapeDataString(specType));

        return basePart + "?" + string.Join("&", pairs) + fragment;
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

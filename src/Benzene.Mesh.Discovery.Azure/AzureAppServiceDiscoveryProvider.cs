using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Discovery.Azure;

/// <summary>
/// Discovers Benzene services by enumerating the Azure App Service / Function App resources
/// (<c>Microsoft.Web/sites</c>) in a subscription, keeping those that match the
/// <see cref="MeshDiscoveryFilter"/> (by default, carry the <c>benzene</c> tag), and emitting an HTTP
/// registry entry per site at its default hostname
/// (<c>https://{host}/benzene/spec|health</c>). Because App Services are HTTP-addressable, the entries
/// use the default HTTP interrogation source (<see cref="MeshServiceSource.Http"/>) — the aggregator's
/// existing <c>HttpMeshServiceSource</c> interrogates each, so no Azure-specific fetch source is needed.
/// </summary>
/// <remarks>
/// This is the Azure analogue of <c>Benzene.Mesh.Discovery.Aws</c>. The mesh's identity needs
/// <c>Reader</c> on the resources it enumerates. A site's host defaults to
/// <c>{name}.azurewebsites.net</c>; override it (custom domain) with the <c>benzene:host</c> tag, and
/// the spec/health paths with <c>benzene:spec-path</c>/<c>benzene:health-path</c>.
/// </remarks>
public class AzureAppServiceDiscoveryProvider : IMeshDiscoveryProvider
{
    /// <summary>Tag overriding the host (default <c>{name}.azurewebsites.net</c>).</summary>
    public const string HostTag = "benzene:host";

    /// <summary>Tag overriding the spec path (default <c>/benzene/spec?type=benzene</c>).</summary>
    public const string SpecPathTag = "benzene:spec-path";

    /// <summary>Tag overriding the health path (default <c>/benzene/health</c>).</summary>
    public const string HealthPathTag = "benzene:health-path";

    private const string DefaultSpecPath = "/benzene/spec?type=benzene";
    private const string DefaultHealthPath = "/benzene/health";

    private readonly IAzureResourceLister _lister;

    /// <summary>Initializes the provider over an Azure resource lister.</summary>
    /// <param name="lister">The port used to list App Service / Function App resources.</param>
    public AzureAppServiceDiscoveryProvider(IAzureResourceLister lister)
    {
        _lister = lister;
    }

    /// <inheritdoc />
    public string Key => "Azure";

    /// <inheritdoc />
    public async Task<IReadOnlyList<MeshServiceRegistryEntry>> DiscoverAsync(
        MeshDiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        var resources = await _lister.ListWebAppsAsync(cancellationToken);

        var entries = new List<MeshServiceRegistryEntry>();
        foreach (var resource in resources)
        {
            if (!filter.Matches(resource.Tags))
            {
                continue;
            }

            if (filter.Regions != null && resource.Location != null &&
                !filter.Regions.Any(region => string.Equals(region, resource.Location, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var defaultHost = resource.DefaultHost ?? $"{resource.Name}.azurewebsites.net";
            var host = SanitizeHost(TagOrDefault(resource.Tags, HostTag, defaultHost), defaultHost);
            var specPath = SanitizePath(TagOrDefault(resource.Tags, SpecPathTag, DefaultSpecPath), DefaultSpecPath);
            var healthPath = SanitizePath(TagOrDefault(resource.Tags, HealthPathTag, DefaultHealthPath), DefaultHealthPath);

            entries.Add(new MeshServiceRegistryEntry(
                resource.Name,
                specUrl: $"https://{host}{specPath}",
                healthUrl: $"https://{host}{healthPath}"));
        }

        return entries;
    }

    private static string TagOrDefault(IReadOnlyDictionary<string, string> tags, string key, string fallback)
        => tags.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    // A host tag carries an attacker-influenceable value (anyone who can tag the resource). Reject
    // anything that isn't a bare host[:port] authority: a value carrying a path, scheme, userinfo, or
    // whitespace/CRLF could otherwise restructure the "https://{host}{path}" URL and point the
    // aggregator's fetch at a different host (SSRF). An invalid override falls back to the safe
    // default host rather than aborting the whole discovery sweep.
    private static string SanitizeHost(string host, string fallback)
        => IsBareAuthority(host) ? host : fallback;

    private static bool IsBareAuthority(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var c in value)
        {
            if (c is '/' or '\\' or '@' or '?' or '#' || char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }

    // A path override must be a real path (start with '/'); otherwise "{host}{path}" would graft the
    // value onto the host - e.g. ".evil.com/spec" makes "myapp.azurewebsites.net.evil.com/spec" - and
    // redirect the fetch off-host. An invalid override falls back to the safe default path.
    private static string SanitizePath(string path, string fallback)
        => path.StartsWith('/') ? path : fallback;
}

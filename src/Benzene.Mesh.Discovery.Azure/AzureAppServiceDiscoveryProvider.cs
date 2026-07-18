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

            var host = TagOrDefault(resource.Tags, HostTag,
                resource.DefaultHost ?? $"{resource.Name}.azurewebsites.net");
            var specPath = TagOrDefault(resource.Tags, SpecPathTag, DefaultSpecPath);
            var healthPath = TagOrDefault(resource.Tags, HealthPathTag, DefaultHealthPath);

            entries.Add(new MeshServiceRegistryEntry(
                resource.Name,
                specUrl: $"https://{host}{specPath}",
                healthUrl: $"https://{host}{healthPath}"));
        }

        return entries;
    }

    private static string TagOrDefault(IReadOnlyDictionary<string, string> tags, string key, string fallback)
        => tags.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}

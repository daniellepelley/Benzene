using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Discovery.Kubernetes;

/// <summary>
/// Discovers Benzene services by listing the Kubernetes Services that match the
/// <see cref="MeshDiscoveryFilter"/> (by default, carry the <c>benzene</c> label) via the Kubernetes
/// API, and emitting an HTTP registry entry per Service pointing at its in-cluster DNS
/// (<c>http://{name}.{namespace}.svc.cluster.local[:port]/benzene/spec|health</c>). Because the
/// services are HTTP-addressable in-cluster, the entries use the default HTTP interrogation source
/// (<see cref="MeshServiceSource.Http"/>) — the existing <c>HttpMeshServiceSource</c> interrogates
/// each one, so no Kubernetes-specific fetch source is needed.
/// </summary>
/// <remarks>
/// The mesh's ServiceAccount needs RBAC to <c>list</c> Services in the target namespace(s)
/// (<c>get</c>/<c>list</c> on <c>services</c>). Spec/health paths follow the Cloud Service Profile
/// defaults; a Service can override them with the <c>benzene.spec-path</c>/<c>benzene.health-path</c>
/// labels, and its scheme/port with <c>benzene.scheme</c> (a Service that terminates TLS itself).
/// </remarks>
public class KubernetesServiceDiscoveryProvider : IMeshDiscoveryProvider
{
    /// <summary>Label overriding the spec path (default <c>/benzene/spec?type=benzene</c>).</summary>
    public const string SpecPathLabel = "benzene.spec-path";

    /// <summary>Label overriding the health path (default <c>/benzene/health</c>).</summary>
    public const string HealthPathLabel = "benzene.health-path";

    /// <summary>Label overriding the URL scheme (default <c>http</c>).</summary>
    public const string SchemeLabel = "benzene.scheme";

    private const string DefaultSpecPath = "/benzene/spec?type=benzene";
    private const string DefaultHealthPath = "/benzene/health";

    private readonly IKubernetesServiceLister _lister;

    /// <summary>Initializes the provider over a Kubernetes Service lister.</summary>
    /// <param name="lister">The port used to list Services from the Kubernetes API.</param>
    public KubernetesServiceDiscoveryProvider(IKubernetesServiceLister lister)
    {
        _lister = lister;
    }

    /// <inheritdoc />
    public string Key => "Kubernetes";

    /// <inheritdoc />
    public async Task<IReadOnlyList<MeshServiceRegistryEntry>> DiscoverAsync(
        MeshDiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        var services = await _lister.ListServicesAsync(filter.Namespace, BuildLabelSelector(filter), cancellationToken);

        var entries = new List<MeshServiceRegistryEntry>(services.Count);
        foreach (var service in services)
        {
            // The lister already applied the label selector server-side, but re-check locally so the
            // provider honours the filter's exact-value semantics uniformly across platforms.
            if (!filter.Matches(service.Labels))
            {
                continue;
            }

            var scheme = LabelOrDefault(service.Labels, SchemeLabel, "http");
            var authority = $"{service.Name}.{service.Namespace}.svc.cluster.local{PortSuffix(scheme, service.Port)}";
            var specPath = LabelOrDefault(service.Labels, SpecPathLabel, DefaultSpecPath);
            var healthPath = LabelOrDefault(service.Labels, HealthPathLabel, DefaultHealthPath);

            entries.Add(new MeshServiceRegistryEntry(
                service.Name,
                specUrl: $"{scheme}://{authority}{specPath}",
                healthUrl: $"{scheme}://{authority}{healthPath}"));
        }

        return entries;
    }

    /// <summary>
    /// Builds a Kubernetes label selector from the filter's tags: <c>key</c> (present with any value)
    /// or <c>key=value</c> (exact match), comma-joined — the same semantics Kubernetes applies natively.
    /// </summary>
    public static string BuildLabelSelector(MeshDiscoveryFilter filter)
    {
        return string.Join(",", filter.Tags.Select(tag =>
            tag.Value == null ? tag.Key : $"{tag.Key}={tag.Value}"));
    }

    private static string PortSuffix(string scheme, int port)
    {
        var defaultPort = string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        return port == defaultPort || port <= 0 ? "" : $":{port}";
    }

    private static string LabelOrDefault(IReadOnlyDictionary<string, string> labels, string key, string fallback)
        => labels.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}

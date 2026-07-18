namespace Benzene.Mesh.Discovery.Kubernetes;

/// <summary>
/// A minimal port over the Kubernetes API for listing Services, so
/// <see cref="KubernetesServiceDiscoveryProvider"/>'s discovery logic (label-selector construction,
/// URL building, filtering) is unit-testable without the Kubernetes client SDK. The real
/// implementation is <see cref="KubernetesApiServiceLister"/>.
/// </summary>
public interface IKubernetesServiceLister
{
    /// <summary>
    /// Lists Services matching <paramref name="labelSelector"/> in <paramref name="namespace"/>
    /// (or across all namespaces when it is <c>null</c>).
    /// </summary>
    Task<IReadOnlyList<KubernetesServiceInfo>> ListServicesAsync(
        string? @namespace, string labelSelector, CancellationToken cancellationToken = default);
}

/// <summary>The subset of a Kubernetes Service that mesh discovery needs.</summary>
/// <param name="Name">The Service's metadata name (used as the mesh service name and DNS host).</param>
/// <param name="Namespace">The Service's namespace (part of its in-cluster DNS).</param>
/// <param name="Port">The Service's first exposed port (the port its cluster DNS resolves traffic to).</param>
/// <param name="Labels">The Service's labels (matched against the discovery filter).</param>
public record KubernetesServiceInfo(
    string Name,
    string Namespace,
    int Port,
    IReadOnlyDictionary<string, string> Labels);

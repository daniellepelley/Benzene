using k8s;
using k8s.Models;

namespace Benzene.Mesh.Discovery.Kubernetes;

/// <summary>
/// The real <see cref="IKubernetesServiceLister"/> over the Kubernetes client SDK's
/// <see cref="IKubernetes"/>. Kept thin (no discovery logic) so the SDK coupling lives in one place
/// and <see cref="KubernetesServiceDiscoveryProvider"/> stays unit-testable against the port.
/// </summary>
public class KubernetesApiServiceLister : IKubernetesServiceLister
{
    private readonly IKubernetes _client;

    /// <summary>Initializes the lister over a Kubernetes client.</summary>
    /// <param name="client">The Kubernetes API client.</param>
    public KubernetesApiServiceLister(IKubernetes client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KubernetesServiceInfo>> ListServicesAsync(
        string? @namespace, string labelSelector, CancellationToken cancellationToken = default)
    {
        var list = @namespace == null
            ? await _client.CoreV1.ListServiceForAllNamespacesAsync(labelSelector: labelSelector, cancellationToken: cancellationToken)
            : await _client.CoreV1.ListNamespacedServiceAsync(@namespace, labelSelector: labelSelector, cancellationToken: cancellationToken);

        var services = new List<KubernetesServiceInfo>();
        foreach (var service in list.Items ?? new List<V1Service>())
        {
            var name = service.Metadata?.Name;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            services.Add(new KubernetesServiceInfo(
                name,
                service.Metadata?.NamespaceProperty ?? "default",
                service.Spec?.Ports?.FirstOrDefault()?.Port ?? 80,
                service.Metadata?.Labels is { } labels ? new Dictionary<string, string>(labels) : new Dictionary<string, string>()));
        }

        return services;
    }
}

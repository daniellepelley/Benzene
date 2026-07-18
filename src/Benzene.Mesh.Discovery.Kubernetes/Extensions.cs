using Benzene.Abstractions.DI;
using Benzene.Mesh.Contracts;
using k8s;

namespace Benzene.Mesh.Discovery.Kubernetes;

/// <summary>Registration for Kubernetes mesh discovery.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="KubernetesServiceDiscoveryProvider"/> (as an additional
    /// <see cref="IMeshDiscoveryProvider"/>) over an in-cluster <see cref="IKubernetes"/> client, plus
    /// a <see cref="MeshDiscoveryRunner"/> over all registered providers. Mirrors
    /// <c>Benzene.Mesh.Discovery.Aws.AddMeshAwsLambdaDiscovery</c>'s DI shape. Intended for a mesh
    /// running inside the cluster (uses <see cref="KubernetesClientConfiguration.InClusterConfig"/>);
    /// its ServiceAccount needs RBAC to list Services.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    public static IBenzeneServiceContainer AddMeshKubernetesDiscovery(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<IKubernetes>(_ => new k8s.Kubernetes(KubernetesClientConfiguration.InClusterConfig()));
        services.AddSingleton<IKubernetesServiceLister>(resolver =>
            new KubernetesApiServiceLister(resolver.GetService<IKubernetes>()));
        services.AddSingleton<IMeshDiscoveryProvider>(resolver =>
            new KubernetesServiceDiscoveryProvider(resolver.GetService<IKubernetesServiceLister>()));
        services.AddSingleton(resolver => new MeshDiscoveryRunner(resolver.GetServices<IMeshDiscoveryProvider>()));
        return services;
    }
}

using Azure.Identity;
using Azure.ResourceManager;
using Benzene.Abstractions.DI;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Discovery.Azure;

/// <summary>Registration for Azure mesh discovery.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="AzureAppServiceDiscoveryProvider"/> (as an additional
    /// <see cref="IMeshDiscoveryProvider"/>) over an <see cref="ArmClient"/> authenticated with
    /// <see cref="DefaultAzureCredential"/> (managed identity in Azure, the dev credential locally),
    /// plus a <see cref="MeshDiscoveryRunner"/> over all registered providers. Mirrors
    /// <c>Benzene.Mesh.Discovery.Aws.AddMeshAwsLambdaDiscovery</c>'s DI shape. The identity needs
    /// <c>Reader</c> on the resources it enumerates.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    /// <param name="subscriptionId">The subscription to enumerate; null uses the credential's default subscription.</param>
    /// <param name="resourceGroup">If set, discovery is constrained to this resource group (recommended when the identity holds Reader at subscription scope).</param>
    public static IBenzeneServiceContainer AddMeshAzureDiscovery(this IBenzeneServiceContainer services,
        string? subscriptionId = null, string? resourceGroup = null)
    {
        services.AddSingleton(_ => new ArmClient(new DefaultAzureCredential()));
        services.AddSingleton<IAzureResourceLister>(resolver =>
            new AzureArmResourceLister(resolver.GetService<ArmClient>(), subscriptionId, resourceGroup));
        services.AddSingleton<IMeshDiscoveryProvider>(resolver =>
            new AzureAppServiceDiscoveryProvider(resolver.GetService<IAzureResourceLister>()));
        services.AddSingleton(resolver => new MeshDiscoveryRunner(resolver.GetServices<IMeshDiscoveryProvider>()));
        return services;
    }
}

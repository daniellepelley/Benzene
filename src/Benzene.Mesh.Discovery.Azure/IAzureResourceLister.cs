namespace Benzene.Mesh.Discovery.Azure;

/// <summary>
/// A minimal port over Azure Resource Manager for listing the App Service / Function App resources
/// (<c>Microsoft.Web/sites</c>) in a subscription, so <see cref="AzureAppServiceDiscoveryProvider"/>'s
/// discovery logic (tag/region filtering, URL building) is unit-testable without the Azure SDK. The
/// real implementation is <see cref="AzureArmResourceLister"/>.
/// </summary>
public interface IAzureResourceLister
{
    /// <summary>Lists the <c>Microsoft.Web/sites</c> resources in the subscription.</summary>
    Task<IReadOnlyList<AzureResourceInfo>> ListWebAppsAsync(CancellationToken cancellationToken = default);
}

/// <summary>The subset of an Azure App Service / Function App resource that mesh discovery needs.</summary>
/// <param name="Name">The resource name (used as the mesh service name and, by convention, its default host).</param>
/// <param name="Location">The Azure region the resource is in (for region scoping), or null if unknown.</param>
/// <param name="DefaultHost">The resource's default hostname if known (e.g. <c>orders.azurewebsites.net</c>), else null.</param>
/// <param name="Tags">The resource's tags (matched against the discovery filter).</param>
public record AzureResourceInfo(
    string Name,
    string? Location,
    string? DefaultHost,
    IReadOnlyDictionary<string, string> Tags);

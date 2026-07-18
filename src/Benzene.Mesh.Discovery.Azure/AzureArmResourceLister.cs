using Azure.ResourceManager;

namespace Benzene.Mesh.Discovery.Azure;

/// <summary>
/// The real <see cref="IAzureResourceLister"/> over Azure Resource Manager (<see cref="ArmClient"/>).
/// Kept thin (no discovery logic) so the SDK coupling lives in one place and
/// <see cref="AzureAppServiceDiscoveryProvider"/> stays unit-testable against the port. Enumerates the
/// <c>Microsoft.Web/sites</c> resources in the default subscription.
/// </summary>
public class AzureArmResourceLister : IAzureResourceLister
{
    private readonly ArmClient _client;

    /// <summary>Initializes the lister over an Azure Resource Manager client.</summary>
    /// <param name="client">The ARM client.</param>
    public AzureArmResourceLister(ArmClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AzureResourceInfo>> ListWebAppsAsync(CancellationToken cancellationToken = default)
    {
        var subscription = await _client.GetDefaultSubscriptionAsync(cancellationToken);

        var resources = new List<AzureResourceInfo>();
        await foreach (var resource in subscription.GetGenericResourcesAsync(
                           filter: "resourceType eq 'Microsoft.Web/sites'", cancellationToken: cancellationToken))
        {
            var data = resource.Data;
            resources.Add(new AzureResourceInfo(
                data.Name,
                data.Location.ToString(),
                DefaultHost: null,
                data.Tags is { } tags ? new Dictionary<string, string>(tags) : new Dictionary<string, string>()));
        }

        return resources;
    }
}

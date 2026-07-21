using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Benzene.Mesh.Discovery.Azure;

/// <summary>
/// The real <see cref="IAzureResourceLister"/> over Azure Resource Manager (<see cref="ArmClient"/>).
/// Kept thin (no discovery logic) so the SDK coupling lives in one place and
/// <see cref="AzureAppServiceDiscoveryProvider"/> stays unit-testable against the port. Enumerates the
/// <c>Microsoft.Web/sites</c> resources in a subscription, optionally scoped to a single subscription id
/// and/or resource group.
/// </summary>
/// <remarks>
/// By default the scope is whatever <c>GetDefaultSubscriptionAsync</c> returns (the first subscription
/// the credential can see) - deterministic only when the identity has a role assignment in exactly one
/// subscription. Pass a <c>subscriptionId</c> to pin the scope explicitly, and a <c>resourceGroup</c> to
/// constrain the sweep to one group so a subscription-scoped <c>Reader</c> role doesn't widen the blast
/// radius to every benzene-tagged site in the subscription. Deployment slots are a distinct resource type
/// (<c>Microsoft.Web/sites/slots</c>) and are never returned by this filter; Function Apps are
/// <c>Microsoft.Web/sites</c> (kind <c>functionapp</c>) and <b>are</b> returned when tagged, which is
/// intended - the tag filter is the gate.
/// </remarks>
public class AzureArmResourceLister : IAzureResourceLister
{
    private readonly ArmClient _client;
    private readonly string? _subscriptionId;
    private readonly string? _resourceGroup;

    /// <summary>Initializes the lister over an Azure Resource Manager client.</summary>
    /// <param name="client">The ARM client.</param>
    /// <param name="subscriptionId">The subscription to enumerate; null uses the credential's default subscription.</param>
    /// <param name="resourceGroup">If set, only sites in this resource group are returned (matched case-insensitively).</param>
    public AzureArmResourceLister(ArmClient client, string? subscriptionId = null, string? resourceGroup = null)
    {
        _client = client;
        _subscriptionId = string.IsNullOrWhiteSpace(subscriptionId) ? null : subscriptionId;
        _resourceGroup = string.IsNullOrWhiteSpace(resourceGroup) ? null : resourceGroup;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AzureResourceInfo>> ListWebAppsAsync(CancellationToken cancellationToken = default)
    {
        var subscription = _subscriptionId != null
            ? _client.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(_subscriptionId))
            : await _client.GetDefaultSubscriptionAsync(cancellationToken);

        var resources = new List<AzureResourceInfo>();
        await foreach (var resource in subscription.GetGenericResourcesAsync(
                           filter: "resourceType eq 'Microsoft.Web/sites'", cancellationToken: cancellationToken))
        {
            // Scope to one resource group in code (the generic-resources $filter doesn't take a resource
            // group), so a subscription-scoped Reader identity still only discovers the intended group.
            if (_resourceGroup != null &&
                !string.Equals(resource.Id.ResourceGroupName, _resourceGroup, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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

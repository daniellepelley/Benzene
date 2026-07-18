using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Azure;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test.Discovery;

public class AzureAppServiceDiscoveryProviderTest
{
    private static AzureResourceInfo Site(string name, string location = "westeurope", params (string, string)[] tags)
        => new(name, location, null, tags.ToDictionary(t => t.Item1, t => t.Item2));

    private static Mock<IAzureResourceLister> ListerWith(params AzureResourceInfo[] sites)
    {
        var mock = new Mock<IAzureResourceLister>();
        mock.Setup(x => x.ListWebAppsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sites);
        return mock;
    }

    [Fact]
    public async Task Discover_EmitsHttpEntriesAtDefaultAzureHost()
    {
        var lister = ListerWith(
            Site("orders", tags: ("benzene", "true")),
            Site("unrelated", tags: ("team", "x")));

        var provider = new AzureAppServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("orders", entry.Name);
        Assert.Equal(MeshServiceSource.Http, entry.Source);
        Assert.Equal("https://orders.azurewebsites.net/benzene/spec?type=benzene", entry.SpecUrl);
        Assert.Equal("https://orders.azurewebsites.net/benzene/health", entry.HealthUrl);
    }

    [Fact]
    public async Task Discover_RespectsHostAndPathTagOverrides()
    {
        var lister = ListerWith(Site("orders", "westeurope",
            ("benzene", "true"),
            (AzureAppServiceDiscoveryProvider.HostTag, "orders.contoso.com"),
            (AzureAppServiceDiscoveryProvider.SpecPathTag, "/x/spec")));

        var provider = new AzureAppServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("https://orders.contoso.com/x/spec", entry.SpecUrl);
        Assert.Equal("https://orders.contoso.com/benzene/health", entry.HealthUrl);
    }

    [Fact]
    public async Task Discover_RegionFilter_ExcludesOtherRegions()
    {
        var lister = ListerWith(
            Site("eu-svc", "westeurope", ("benzene", "true")),
            Site("us-svc", "eastus", ("benzene", "true")));

        var provider = new AzureAppServiceDiscoveryProvider(lister.Object);
        var entries = await provider.DiscoverAsync(
            new MeshDiscoveryFilter(regions: new[] { "westeurope" }));

        Assert.Equal("eu-svc", Assert.Single(entries).Name);
    }

    [Fact]
    public async Task Discover_ValuedTagFilter_ExcludesNonMatching()
    {
        var lister = ListerWith(
            Site("prod-svc", tags: ("benzene", "prod")),
            Site("dev-svc", tags: ("benzene", "dev")));

        var provider = new AzureAppServiceDiscoveryProvider(lister.Object);
        var entries = await provider.DiscoverAsync(
            new MeshDiscoveryFilter(new Dictionary<string, string?> { ["benzene"] = "prod" }));

        Assert.Equal("prod-svc", Assert.Single(entries).Name);
    }
}

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
    public async Task Discover_HostTagRestructuringTheUrl_FallsBackToDefaultHost()
    {
        // A host tag carrying a path/scheme/userinfo could restructure "https://{host}{path}" and
        // redirect the aggregator's fetch off-host (SSRF). Such a value is rejected in favour of the
        // safe default host; only a bare host[:port] authority is honoured.
        var lister = ListerWith(Site("orders", "westeurope",
            ("benzene", "true"),
            (AzureAppServiceDiscoveryProvider.HostTag, "evil.com/@orders.azurewebsites.net")));

        var provider = new AzureAppServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("https://orders.azurewebsites.net/benzene/spec?type=benzene", entry.SpecUrl);
    }

    [Fact]
    public async Task Discover_SpecPathTagNotStartingWithSlash_FallsBackToDefault()
    {
        // A path override that doesn't start with '/' would graft onto the host
        // ("orders.azurewebsites.net.evil.com/...") and redirect off-host; it falls back to default.
        var lister = ListerWith(Site("orders", "westeurope",
            ("benzene", "true"),
            (AzureAppServiceDiscoveryProvider.SpecPathTag, ".evil.com/spec")));

        var provider = new AzureAppServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("https://orders.azurewebsites.net/benzene/spec?type=benzene", entry.SpecUrl);
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

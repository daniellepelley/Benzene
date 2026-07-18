using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Discovery.Kubernetes;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test.Discovery;

public class KubernetesServiceDiscoveryProviderTest
{
    private static KubernetesServiceInfo Svc(string name, int port = 80, params (string, string)[] labels)
        => new(name, "default", port, labels.ToDictionary(l => l.Item1, l => l.Item2));

    private static Mock<IKubernetesServiceLister> ListerWith(params KubernetesServiceInfo[] services)
    {
        var mock = new Mock<IKubernetesServiceLister>();
        mock.Setup(x => x.ListServicesAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(services);
        return mock;
    }

    [Fact]
    public async Task Discover_EmitsHttpEntriesAtInClusterDns()
    {
        var lister = ListerWith(Svc("orders", 80, ("benzene", "true")));

        var provider = new KubernetesServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("orders", entry.Name);
        Assert.Equal(MeshServiceSource.Http, entry.Source);
        Assert.Equal("http://orders.default.svc.cluster.local/benzene/spec?type=benzene", entry.SpecUrl);
        Assert.Equal("http://orders.default.svc.cluster.local/benzene/health", entry.HealthUrl);
    }

    [Fact]
    public async Task Discover_NonDefaultPort_IsIncludedInAuthority()
    {
        var lister = ListerWith(Svc("payments", 8080, ("benzene", "true")));

        var provider = new KubernetesServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("http://payments.default.svc.cluster.local:8080/benzene/spec?type=benzene", entry.SpecUrl);
    }

    [Fact]
    public async Task Discover_RespectsPathAndSchemeLabelOverrides()
    {
        var lister = ListerWith(Svc("shipping", 443,
            ("benzene", "true"),
            (KubernetesServiceDiscoveryProvider.SchemeLabel, "https"),
            (KubernetesServiceDiscoveryProvider.SpecPathLabel, "/x/spec"),
            (KubernetesServiceDiscoveryProvider.HealthPathLabel, "/x/health")));

        var provider = new KubernetesServiceDiscoveryProvider(lister.Object);
        var entry = Assert.Single(await provider.DiscoverAsync(new MeshDiscoveryFilter()));

        Assert.Equal("https://shipping.default.svc.cluster.local/x/spec", entry.SpecUrl);
        Assert.Equal("https://shipping.default.svc.cluster.local/x/health", entry.HealthUrl);
    }

    [Fact]
    public async Task Discover_ValuedTagFilter_ExcludesNonMatching()
    {
        var lister = ListerWith(
            Svc("prod-svc", 80, ("benzene", "prod")),
            Svc("dev-svc", 80, ("benzene", "dev")));

        var provider = new KubernetesServiceDiscoveryProvider(lister.Object);
        var entries = await provider.DiscoverAsync(
            new MeshDiscoveryFilter(new Dictionary<string, string?> { ["benzene"] = "prod" }));

        Assert.Equal("prod-svc", Assert.Single(entries).Name);
    }

    [Fact]
    public void BuildLabelSelector_PresentAndValued()
    {
        Assert.Equal("benzene", KubernetesServiceDiscoveryProvider.BuildLabelSelector(new MeshDiscoveryFilter()));
        Assert.Equal("benzene=prod", KubernetesServiceDiscoveryProvider.BuildLabelSelector(
            new MeshDiscoveryFilter(new Dictionary<string, string?> { ["benzene"] = "prod" })));
    }

    [Fact]
    public async Task Discover_PassesNamespaceAndSelectorToLister()
    {
        var lister = new Mock<IKubernetesServiceLister>();
        lister.Setup(x => x.ListServicesAsync("mesh-ns", "benzene", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Svc("orders", 80, ("benzene", "true")) });

        var provider = new KubernetesServiceDiscoveryProvider(lister.Object);
        var entries = await provider.DiscoverAsync(new MeshDiscoveryFilter(@namespace: "mesh-ns"));

        Assert.Single(entries);
        lister.Verify(x => x.ListServicesAsync("mesh-ns", "benzene", It.IsAny<CancellationToken>()), Times.Once);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test.Discovery;

public class MeshDiscoveryRunnerTest
{
    private class FakeProvider : IMeshDiscoveryProvider
    {
        private readonly MeshServiceRegistryEntry[] _entries;
        public FakeProvider(string key, params MeshServiceRegistryEntry[] entries) { Key = key; _entries = entries; }
        public string Key { get; }
        public Task<IReadOnlyList<MeshServiceRegistryEntry>> DiscoverAsync(MeshDiscoveryFilter filter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MeshServiceRegistryEntry>>(_entries);
    }

    private static MeshServiceRegistryEntry Lambda(string name)
        => new(name, "", "", MeshServiceSource.AwsLambdaInvoke, new Dictionary<string, string> { ["functionName"] = name });

    [Fact]
    public async Task Discover_UnionsProvidersAndSeed()
    {
        var runner = new MeshDiscoveryRunner(new[]
        {
            new FakeProvider("aws", Lambda("orders"), Lambda("billing")),
        });
        var seed = new MeshServiceRegistry(new[] { new MeshServiceRegistryEntry("legacy", "https://legacy/spec", "https://legacy/health") });

        var registry = await runner.DiscoverAsync(new MeshDiscoveryFilter(), seed);

        Assert.Equal(new[] { "billing", "legacy", "orders" }, registry.Services.Select(s => s.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task Discover_StaticSeedWinsOnNameClash()
    {
        // A hand-pinned entry is an intentional override; discovery must not replace it.
        var runner = new MeshDiscoveryRunner(new[] { new FakeProvider("aws", Lambda("orders")) });
        var seed = new MeshServiceRegistry(new[] { new MeshServiceRegistryEntry("orders", "https://pinned/spec", "https://pinned/health") });

        var registry = await runner.DiscoverAsync(new MeshDiscoveryFilter(), seed);

        var orders = Assert.Single(registry.Services);
        Assert.Equal(MeshServiceSource.Http, orders.Source);        // the pinned HTTP entry, not the discovered Lambda one
        Assert.Equal("https://pinned/spec", orders.SpecUrl);
    }

    [Fact]
    public async Task Discover_NoProvidersNoSeed_IsEmpty()
    {
        var runner = new MeshDiscoveryRunner(System.Array.Empty<IMeshDiscoveryProvider>());

        var registry = await runner.DiscoverAsync(new MeshDiscoveryFilter());

        Assert.Empty(registry.Services);
    }

    [Fact]
    public void RegistryJson_RoundTripsThroughTheMeshJsonShape()
    {
        var registry = new MeshServiceRegistry(new[]
        {
            Lambda("orders"),
            new MeshServiceRegistryEntry("legacy", "https://legacy/spec", "https://legacy/health"),
        });

        var json = MeshRegistryJson.Serialize(registry);
        var back = MeshRegistryJson.Deserialize(json);

        Assert.Contains("\"services\"", json);
        Assert.Contains("\"functionName\": \"orders\"", json);      // camelCase, mesh.json shape
        Assert.Equal(2, back.Services.Length);
        var orders = back.Services.Single(s => s.Name == "orders");
        Assert.Equal(MeshServiceSource.AwsLambdaInvoke, orders.Source);
        Assert.Equal("orders", orders.SourceOptions!["functionName"]);
    }

    [Fact]
    public void Filter_Matches_RequiresTagPresence_AndExactValueWhenSpecified()
    {
        var defaultFilter = new MeshDiscoveryFilter(); // { benzene: null } => present, any value
        Assert.True(defaultFilter.Matches(new Dictionary<string, string> { ["benzene"] = "true" }));
        Assert.True(defaultFilter.Matches(new Dictionary<string, string> { ["benzene"] = "anything" }));
        Assert.False(defaultFilter.Matches(new Dictionary<string, string> { ["other"] = "x" }));

        var valued = new MeshDiscoveryFilter(new Dictionary<string, string?> { ["benzene"] = "prod" });
        Assert.True(valued.Matches(new Dictionary<string, string> { ["benzene"] = "prod" }));
        Assert.False(valued.Matches(new Dictionary<string, string> { ["benzene"] = "dev" }));
    }
}

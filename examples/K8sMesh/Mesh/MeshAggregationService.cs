using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;

namespace Benzene.Examples.K8sMesh.Mesh;

/// <summary>
/// One discovery + aggregation pass: discover the benzene-labelled Kubernetes Services, write the
/// discovered registry to the artifact store (the "discovery creates the config" seam), interrogate
/// each over HTTP, and write the catalog artifacts. Shared by the on-demand <c>POST /mesh/refresh</c>
/// endpoint and the periodic background service.
/// </summary>
public class MeshAggregationService
{
    private readonly MeshDiscoveryRunner _discovery;
    private readonly IMeshArtifactStore _store;
    private readonly MeshAggregator _aggregator;

    public MeshAggregationService(MeshDiscoveryRunner discovery, IMeshArtifactStore store, MeshAggregator aggregator)
    {
        _discovery = discovery;
        _store = store;
        _aggregator = aggregator;
    }

    /// <summary>Runs a pass and returns the number of services discovered.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var ns = Environment.GetEnvironmentVariable("MESH_NAMESPACE");
        var filter = new MeshDiscoveryFilter(@namespace: string.IsNullOrWhiteSpace(ns) ? null : ns);

        var registry = await _discovery.DiscoverAsync(filter, cancellationToken: cancellationToken);
        await _store.PublishAsync("registry.json", MeshRegistryJson.Serialize(registry));
        await _aggregator.RunOnceAsync(registry);
        return registry.Services.Length;
    }
}

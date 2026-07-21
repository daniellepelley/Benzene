using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;

namespace Benzene.Examples.AzureMesh.Mesh;

/// <summary>
/// One discovery + aggregation pass: discover the benzene-tagged Azure App Services, write the
/// discovered registry to Blob Storage (the "discovery creates the config" seam), interrogate each
/// over HTTPS, and write the catalog artifacts. Shared by the on-demand <c>POST /mesh/refresh</c>
/// endpoint and the periodic background service.
/// </summary>
public class MeshAggregationService
{
    private readonly MeshDiscoveryRunner _discovery;
    private readonly IMeshArtifactStore _store;
    private readonly MeshAggregator _aggregator;

    // Single-writer gate: the 30s background pass and an on-demand POST /mesh/refresh both call RunAsync
    // against the same remote Blob store. Without this, two overlapping passes can interleave their writes
    // (manifest.json from one, services/*.json from the other) and leave a momentarily inconsistent
    // catalog. The service is a singleton, so one semaphore serialises every pass.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MeshAggregationService(MeshDiscoveryRunner discovery, IMeshArtifactStore store, MeshAggregator aggregator)
    {
        _discovery = discovery;
        _store = store;
        _aggregator = aggregator;
    }

    /// <summary>Runs a pass and returns the number of services discovered.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var region = Environment.GetEnvironmentVariable("MESH_REGION");
            var filter = new MeshDiscoveryFilter(
                regions: string.IsNullOrWhiteSpace(region) ? null : new[] { region });

            var registry = await _discovery.DiscoverAsync(filter, cancellationToken: cancellationToken);
            await _store.PublishAsync("registry.json", MeshRegistryJson.Serialize(registry));
            await _aggregator.RunOnceAsync(registry);
            return registry.Services.Length;
        }
        finally
        {
            _gate.Release();
        }
    }
}

using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;

namespace Benzene.Examples.GoogleCloudMesh.Mesh;

/// <summary>
/// One aggregation pass: write the static registry, interrogate each service over HTTPS (the generic
/// <c>HttpMeshServiceSource</c>), and write the catalog artifacts to Google Cloud Storage. Driven by
/// the on-demand <c>POST /mesh/refresh</c> endpoint (which Cloud Scheduler hits periodically — Cloud
/// Functions has no timer trigger).
/// </summary>
public class MeshAggregationService
{
    private readonly IMeshArtifactStore _store;
    private readonly MeshAggregator _aggregator;

    // Single-writer gate: an on-demand refresh and a scheduled one both write the same GCS objects.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MeshAggregationService(IMeshArtifactStore store, MeshAggregator aggregator)
    {
        _store = store;
        _aggregator = aggregator;
    }

    /// <summary>Runs a pass and returns the number of services in the registry.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var registry = MeshRegistry.FromEnvironment();
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

using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AwsMesh.Mesh;

/// <summary>
/// The discovery + aggregation pass. Discovers the benzene-tagged Lambdas, writes the discovered
/// registry to S3 (the "discovery creates the config" seam), interrogates each discovered Lambda via
/// the aggregator (Lambda-Invoke), and writes the catalog artifacts to S3. Triggered on a schedule by
/// EventBridge (detail-type <c>mesh:aggregate</c>) or on demand by <c>POST /mesh/refresh</c>.
/// </summary>
[Message("mesh:aggregate")]
[HttpEndpoint("POST", "/mesh/refresh")]
public class MeshAggregateHandler : IMessageHandler<Void, MeshAggregateSummary>
{
    private readonly MeshDiscoveryRunner _discovery;
    private readonly IMeshArtifactStore _store;
    private readonly MeshAggregator _aggregator;

    public MeshAggregateHandler(MeshDiscoveryRunner discovery, IMeshArtifactStore store, MeshAggregator aggregator)
    {
        _discovery = discovery;
        _store = store;
        _aggregator = aggregator;
    }

    public async Task<IBenzeneResult<MeshAggregateSummary>> HandleAsync(Void request)
    {
        // 1. Discover benzene-tagged services (default filter) and persist the config to S3.
        var registry = await _discovery.DiscoverAsync(new MeshDiscoveryFilter());
        await _store.PublishAsync("registry.json", MeshRegistryJson.Serialize(registry));

        // 2. Interrogate each discovered service and publish the catalog artifacts to S3.
        await _aggregator.RunOnceAsync(registry);

        // A pass creates/refreshes the catalog artifacts (a state change), so signal 201 rather than
        // 200 on the POST /mesh/refresh surface. (On the EventBridge path the status is irrelevant.)
        return BenzeneResult.Created(new MeshAggregateSummary(registry.Services.Length));
    }
}

/// <summary>The outcome of a mesh aggregation pass.</summary>
public class MeshAggregateSummary
{
    public MeshAggregateSummary(int discovered)
    {
        Discovered = discovered;
    }

    /// <summary>The number of services discovered and catalogued.</summary>
    public int Discovered { get; }
}

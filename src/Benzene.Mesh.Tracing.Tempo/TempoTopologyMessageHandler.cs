using System.Text.Json;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>
/// Exposes <see cref="TempoServiceGraphTopologyBuilder.BuildAsync"/> as a Benzene message handler
/// and publishes the result as <c>topology.json</c> - reachable on whatever transport the host
/// already runs, the same "dogfooded" shape as
/// <c>Benzene.Mesh.Aggregator.MeshAggregateMessageHandler</c>.
/// </summary>
[HttpEndpoint("POST", "/mesh/topology")]
[Message("mesh:topology")]
public class TempoTopologyMessageHandler : IMessageHandler<Void, MeshTopology>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly TempoServiceGraphTopologyBuilder _builder;
    private readonly IMeshArtifactStore _store;

    /// <summary>Initializes a new instance of the <see cref="TempoTopologyMessageHandler"/> class.</summary>
    /// <param name="builder">Builds the topology from Tempo's service-graph metrics.</param>
    /// <param name="store">
    /// Where <c>topology.json</c> is published - expected to be the same store an already-registered
    /// <c>Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator</c> call writes
    /// <c>manifest.json</c>/<c>services/*.json</c> to (see <see cref="Extensions.AddTempoTopology"/>).
    /// </param>
    public TempoTopologyMessageHandler(TempoServiceGraphTopologyBuilder builder, IMeshArtifactStore store)
    {
        _builder = builder;
        _store = store;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult<MeshTopology>> HandleAsync(Void request)
    {
        var topology = await _builder.BuildAsync();
        await _store.PublishAsync("topology.json", JsonSerializer.Serialize(topology, JsonOptions));

        return BenzeneResult.Ok(topology);
    }
}

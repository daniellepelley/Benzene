using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Mesh.Contracts;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Exposes <see cref="MeshAggregator.RunOnceAsync"/> as a Benzene message handler - reachable on
/// whatever transport the host already runs (HTTP, a scheduled Lambda/Function invocation, a queue
/// message) with no bespoke hosting code, the same way any other Benzene service is invoked.
/// </summary>
[HttpEndpoint("POST", "/mesh/aggregate")]
[Message("mesh:aggregate")]
public class MeshAggregateMessageHandler : IMessageHandler<Void, MeshManifest>
{
    private readonly MeshAggregator _aggregator;
    private readonly MeshServiceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="MeshAggregateMessageHandler"/> class.</summary>
    /// <param name="aggregator">The aggregator to run.</param>
    /// <param name="registry">The registry of services to poll.</param>
    public MeshAggregateMessageHandler(MeshAggregator aggregator, MeshServiceRegistry registry)
    {
        _aggregator = aggregator;
        _registry = registry;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult<MeshManifest>> HandleAsync(Void request)
    {
        var manifest = await _aggregator.RunOnceAsync(_registry);
        return BenzeneResult.Ok(manifest);
    }
}

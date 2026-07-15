using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// The push/self-report ingestion endpoint - accepts a <see cref="MeshServiceReport"/> and hands it
/// to whichever <see cref="IMeshReportPublisher"/> is registered (<see cref="ArtifactStoreMeshReportPublisher"/>
/// by default, via <c>Extensions.AddMeshAggregator</c>). Reachable on whatever transport the host
/// already runs, the same dogfooded shape as <see cref="MeshAggregateMessageHandler"/>.
/// </summary>
/// <remarks>
/// Only actually reachable if the consuming host's own <c>.AddMessageHandlers()</c>/<c>.UseMessageHandlers()</c>
/// call discovers it (the same opt-in every Benzene message handler already requires) - an
/// aggregator deployment that never wires this up simply has no write surface, preserving
/// "aggregator polls, UI reads static files" as the default, not a fait accompli.
/// </remarks>
[HttpEndpoint("POST", "/mesh/report")]
[Message("mesh:report")]
public class MeshReportMessageHandler : IMessageHandler<MeshServiceReport>
{
    private readonly IMeshReportPublisher _publisher;

    /// <summary>Initializes a new instance of the <see cref="MeshReportMessageHandler"/> class.</summary>
    /// <param name="publisher">Publishes the incoming report into the mesh catalog.</param>
    public MeshReportMessageHandler(IMeshReportPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <inheritdoc />
    public Task HandleAsync(MeshServiceReport request) => _publisher.PublishAsync(request);
}

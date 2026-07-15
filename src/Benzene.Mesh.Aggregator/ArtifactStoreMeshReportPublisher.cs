using System.Text.Json;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// The direct-write <see cref="IMeshReportPublisher"/> - turns a self-reported <see cref="MeshServiceReport"/>
/// into a full <see cref="MeshServiceSnapshot"/> (via the same <see cref="MeshSnapshotBuilder"/> a
/// pulled fetch uses, so both compute <see cref="MeshServiceSnapshot.ContractDrift"/> identically)
/// and writes it straight into the shared <see cref="IMeshArtifactStore"/> - the same
/// <c>services/{name}.json</c> path <see cref="MeshAggregator"/> itself writes to.
/// </summary>
/// <remarks>
/// Fits a reporter colocated with the aggregator's own storage (e.g. sharing a mounted volume in a
/// Docker Compose deployment). A reporter that isn't colocated should use
/// <c>Benzene.Mesh.Reporting.HttpMeshReportPublisher</c> instead.
/// </remarks>
public class ArtifactStoreMeshReportPublisher : IMeshReportPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly IMeshArtifactStore _store;

    /// <summary>Initializes a new instance of the <see cref="ArtifactStoreMeshReportPublisher"/> class.</summary>
    /// <param name="store">Where the resulting snapshot is written.</param>
    public ArtifactStoreMeshReportPublisher(IMeshArtifactStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task PublishAsync(MeshServiceReport report)
    {
        var snapshot = await MeshSnapshotBuilder.BuildAsync(_store, report.Name, report.ReportedAtUtc, report.SpecJson, report.Health, report.Error);
        await _store.PublishAsync($"services/{report.Name}.json", JsonSerializer.Serialize(snapshot, JsonOptions));
    }
}

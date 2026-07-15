namespace Benzene.Mesh.Contracts;

/// <summary>
/// Publishes a self-reported <see cref="MeshServiceReport"/> somewhere it becomes part of the mesh
/// catalog - a port, not an implementation, so a reporting service depends on this (plus
/// <see cref="MeshServiceReport"/>) without pulling in any particular publish mechanism. Lives here
/// rather than alongside <c>IMeshArtifactStore</c> in <c>Benzene.Mesh.Aggregator</c> - a deliberate,
/// small widening of this package's role from "pure data shapes" to "data shapes + zero-I/O port
/// interfaces" - so a lightweight reporting client (<c>Benzene.Mesh.Reporting</c>) depends on just
/// this package, not the whole aggregator.
/// </summary>
/// <remarks>
/// Two implementations ship: <c>Benzene.Mesh.Aggregator.ArtifactStoreMeshReportPublisher</c> (writes
/// directly into the shared <c>IMeshArtifactStore</c> - for a reporter colocated with the
/// aggregator's own storage) and <c>Benzene.Mesh.Reporting.HttpMeshReportPublisher</c> (POSTs to an
/// aggregator's ingestion endpoint - for a reporter that isn't colocated). Both are swappable
/// behind this one port.
/// </remarks>
public interface IMeshReportPublisher
{
    /// <summary>Publishes the given report.</summary>
    /// <param name="report">The report to publish.</param>
    Task PublishAsync(MeshServiceReport report);
}

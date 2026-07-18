namespace Benzene.Mesh.Contracts;

/// <summary>
/// Enumerates the services that currently exist in some environment (a cloud account, a cluster) and
/// produces the <see cref="MeshServiceRegistryEntry"/> records a <c>Benzene.Mesh.Aggregator</c> would
/// otherwise be hand-fed from a static <c>mesh.json</c>. This is the "self-discovery" seam: an adapter
/// package (AWS/Azure/Kubernetes) implements it to introspect its platform, filtered by tag/label.
/// </summary>
/// <remarks>
/// Discovery is a distinct phase from runtime monitoring: a <see cref="MeshDiscoveryRunner"/> runs the
/// registered providers and writes the resulting registry as JSON config (see
/// <see cref="MeshRegistryJson"/>), which the aggregator then consumes at runtime exactly as it
/// consumes a hand-written <c>mesh.json</c>. Each emitted entry is bound to an existing interrogation
/// source (<see cref="MeshServiceSource"/>) — discovery decides <em>which</em> services exist; the
/// aggregator's <c>IMeshServiceSource</c> decides <em>how to reach</em> each one. Multiple providers
/// compose via <c>IEnumerable&lt;IMeshDiscoveryProvider&gt;</c>, mirroring the source seam.
/// </remarks>
public interface IMeshDiscoveryProvider
{
    /// <summary>A stable key identifying this provider (e.g. the platform it discovers).</summary>
    string Key { get; }

    /// <summary>Enumerates services matching <paramref name="filter"/> and returns their registry entries.</summary>
    /// <param name="filter">The tag/label (and optional region/namespace) filter.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<MeshServiceRegistryEntry>> DiscoverAsync(
        MeshDiscoveryFilter filter, CancellationToken cancellationToken = default);
}

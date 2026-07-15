using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Stand-in <see cref="IMeshServiceSource"/> returned when a <see cref="MeshServiceRegistryEntry.Source"/>
/// has no matching registered source - throws from both fetch methods so a misconfigured single
/// entry surfaces as that service's own <c>Unreachable</c>/error result (via the same try/catch
/// every other fetch failure goes through in <see cref="MeshAggregator"/>), rather than crashing
/// the whole aggregation run.
/// </summary>
internal class UnknownMeshServiceSource : IMeshServiceSource
{
    private readonly string _requestedSource;

    public UnknownMeshServiceSource(string requestedSource)
    {
        _requestedSource = requestedSource;
        Key = requestedSource;
    }

    public string Key { get; }

    public Task<string> FetchSpecAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken) => throw NotRegistered();

    public Task<string> FetchHealthAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken) => throw NotRegistered();

    private InvalidOperationException NotRegistered() =>
        new($"No {nameof(IMeshServiceSource)} registered for source \"{_requestedSource}\".");
}

using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Dispatch;

/// <summary>
/// Dispatches a message to ONE target service over a specific transport - the write-side counterpart of
/// <c>Benzene.Mesh.Aggregator.IMeshServiceSource</c> (which only reads spec/health). Keyed by
/// <see cref="MeshServiceRegistryEntry.Source"/> so the handler picks the right transport per service.
/// </summary>
public interface IMeshServiceDispatcher
{
    /// <summary>The <see cref="MeshServiceRegistryEntry.Source"/> value this dispatcher handles.</summary>
    string Key { get; }

    /// <summary>Sends <paramref name="envelope"/> to the service described by <paramref name="entry"/> and returns its response.</summary>
    Task<MeshDispatchResult> DispatchAsync(MeshServiceRegistryEntry entry, MeshDispatchEnvelope envelope, CancellationToken cancellationToken);
}

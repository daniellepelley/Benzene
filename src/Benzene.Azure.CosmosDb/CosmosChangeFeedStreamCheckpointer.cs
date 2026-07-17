using Benzene.Core.Middleware;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// The change feed's <see cref="IStreamCheckpointer{TItem}"/>: wraps the SDK's batch-level manual
/// checkpoint hook. The change feed has no per-document resume token, so the
/// <c>lastProcessed</c> item is ignored - any call checkpoints the <em>whole delivered batch</em>
/// as a unit. A handler wanting finer-grained safety must therefore only call this once everything
/// in the batch it cares about is safe, and do its own within-batch bookkeeping otherwise - the
/// same coarse granularity documented for the Event Hubs Functions trigger.
/// </summary>
internal class CosmosChangeFeedStreamCheckpointer<TDocument> : IStreamCheckpointer<TDocument>
{
    private readonly Func<Task> _checkpointAsync;

    public CosmosChangeFeedStreamCheckpointer(Func<Task> checkpointAsync)
    {
        _checkpointAsync = checkpointAsync;
    }

    /// <summary>Whether the handler has checkpointed this batch.</summary>
    public bool HasCheckpointed { get; private set; }

    public async Task CheckpointAsync(TDocument lastProcessed)
    {
        await _checkpointAsync();
        HasCheckpointed = true;
    }
}

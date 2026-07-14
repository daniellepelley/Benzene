namespace Benzene.Core.Middleware;

/// <summary>
/// A transport-supplied hook a streaming pipeline can call to checkpoint (acknowledge) progress —
/// telling the underlying runtime "everything up to and including this item has been processed".
/// Transports that checkpoint themselves supply <see cref="NullStreamCheckpointer{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">The type of item flowing through the stream.</typeparam>
public interface IStreamCheckpointer<in TItem>
{
    /// <summary>
    /// Checkpoints progress up to and including <paramref name="lastProcessed"/>.
    /// </summary>
    /// <param name="lastProcessed">The last item successfully processed.</param>
    Task CheckpointAsync(TItem lastProcessed);
}

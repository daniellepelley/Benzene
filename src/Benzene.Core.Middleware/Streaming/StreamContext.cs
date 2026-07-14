namespace Benzene.Core.Middleware;

/// <summary>
/// The pipeline context for a stream: the whole batch/stream presented as one context (fan-in),
/// rather than fanned out into one context per item. Stream middleware consumes <see cref="Items"/>
/// directly, which lets it window, aggregate, order-by-key, and checkpoint — none of which are
/// possible once a batch has been fanned out into isolated per-item contexts.
/// </summary>
/// <typeparam name="TItem">The type of item flowing through the stream.</typeparam>
public class StreamContext<TItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamContext{TItem}"/> class.
    /// </summary>
    /// <param name="items">The stream of items, pulled lazily so the pipeline can apply backpressure.</param>
    /// <param name="checkpointer">The checkpoint hook; defaults to <see cref="NullStreamCheckpointer{TItem}"/>.</param>
    /// <param name="cancellationToken">Cancellation for the stream (the pipeline itself carries none).</param>
    /// <param name="metadata">Optional transport metadata (partition id, consumer group, etc.).</param>
    public StreamContext(
        IAsyncEnumerable<TItem> items,
        IStreamCheckpointer<TItem>? checkpointer = null,
        CancellationToken cancellationToken = default,
        IDictionary<string, object>? metadata = null)
    {
        Items = items;
        Checkpointer = checkpointer ?? NullStreamCheckpointer<TItem>.Instance;
        CancellationToken = cancellationToken;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>The items in the stream, iterated lazily (supports backpressure).</summary>
    public IAsyncEnumerable<TItem> Items { get; }

    /// <summary>The checkpoint hook for acknowledging progress to the transport.</summary>
    public IStreamCheckpointer<TItem> Checkpointer { get; }

    /// <summary>Cancellation for long-lived streams; the pipeline signature carries no token, so it rides here.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Transport metadata that doesn't fit the item shape (partition id, consumer group, …).</summary>
    public IDictionary<string, object> Metadata { get; }
}

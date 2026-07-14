namespace Benzene.Core.Middleware;

/// <summary>
/// A no-op <see cref="IStreamCheckpointer{TItem}"/> — the default for transports that manage
/// checkpointing themselves (e.g. the Azure Functions Event Hubs extension), or when no checkpointing
/// is required.
/// </summary>
/// <typeparam name="TItem">The type of item flowing through the stream.</typeparam>
public class NullStreamCheckpointer<TItem> : IStreamCheckpointer<TItem>
{
    /// <summary>The shared instance.</summary>
    public static readonly NullStreamCheckpointer<TItem> Instance = new();

    /// <inheritdoc />
    public Task CheckpointAsync(TItem lastProcessed) => Task.CompletedTask;
}

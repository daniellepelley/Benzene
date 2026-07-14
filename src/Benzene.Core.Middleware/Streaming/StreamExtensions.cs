using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Pipeline steps for consuming a <see cref="StreamContext{TItem}"/>. These are ordinary middleware,
/// so they compose with every other Benzene solution (correlation, metrics, exception handling) on the
/// same builder.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Adds a terminal stream-processing step that receives the whole <see cref="StreamContext{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of item flowing through the stream.</typeparam>
    /// <param name="app">The stream pipeline builder.</param>
    /// <param name="process">The delegate that consumes the stream context.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<StreamContext<TItem>> UseStream<TItem>(
        this IMiddlewarePipelineBuilder<StreamContext<TItem>> app,
        Func<StreamContext<TItem>, Task> process)
    {
        return app.Use("Stream", async (StreamContext<TItem> context, Func<Task> next) =>
        {
            await process(context);
            await next();
        });
    }

    /// <summary>
    /// Adds a terminal stream-processing step that receives the item stream and cancellation token
    /// directly — the common case when you don't need the rest of the context.
    /// </summary>
    /// <typeparam name="TItem">The type of item flowing through the stream.</typeparam>
    /// <param name="app">The stream pipeline builder.</param>
    /// <param name="process">The delegate that consumes the items.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<StreamContext<TItem>> UseStream<TItem>(
        this IMiddlewarePipelineBuilder<StreamContext<TItem>> app,
        Func<IAsyncEnumerable<TItem>, CancellationToken, Task> process)
    {
        return app.UseStream<TItem>(context => process(context.Items, context.CancellationToken));
    }
}

using System.Text;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.BlobStorage;

/// <summary>
/// Provides pipeline steps for consuming blobs and extension methods for dispatching blob trigger
/// deliveries to a built <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a terminal blob-processing step that receives the <see cref="BlobStorageContext"/> -
    /// the blob counterpart of the streaming engine's <c>UseStream(...)</c> sugar.
    /// </summary>
    /// <param name="app">The blob pipeline builder.</param>
    /// <param name="process">The delegate that consumes the blob context.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<BlobStorageContext> UseBlob(
        this IMiddlewarePipelineBuilder<BlobStorageContext> app,
        Func<BlobStorageContext, Task> process)
    {
        return app.Use("Blob", async (BlobStorageContext context, Func<Task> next) =>
        {
            await process(context);
            await next();
        });
    }

    /// <summary>
    /// Adds a terminal blob-processing step that receives the blob directly - the common case when
    /// you don't need the rest of the context.
    /// </summary>
    /// <param name="app">The blob pipeline builder.</param>
    /// <param name="process">The delegate that consumes the blob.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<BlobStorageContext> UseBlob(
        this IMiddlewarePipelineBuilder<BlobStorageContext> app,
        Func<BlobTriggerEvent, Task> process)
    {
        return app.UseBlob(context => process(context.Blob));
    }

    /// <summary>
    /// Dispatches a blob trigger delivery to the Azure Function app's blob entry point application.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="name">The blob's name (bind the trigger's <c>{name}</c> expression).</param>
    /// <param name="content">The blob's content (bind the trigger parameter as <c>byte[]</c>).</param>
    /// <returns>A task that completes when the blob has been handled.</returns>
    public static Task HandleBlob(this IAzureFunctionApp source, string name, byte[] content)
    {
        return source.HandleAsync(new BlobTriggerEvent(name, content));
    }

    /// <summary>
    /// Dispatches a text blob trigger delivery - bound as <c>string</c> - to the Azure Function
    /// app's blob entry point application, encoding the content as UTF-8.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="name">The blob's name (bind the trigger's <c>{name}</c> expression).</param>
    /// <param name="content">The blob's content as text.</param>
    /// <returns>A task that completes when the blob has been handled.</returns>
    public static Task HandleBlob(this IAzureFunctionApp source, string name, string content)
    {
        return source.HandleBlob(name, Encoding.UTF8.GetBytes(content));
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.CosmosDb;

/// <summary>
/// Adds Cosmos DB Change Feed handling: the whole triggered batch of changed documents is presented to
/// the pipeline as a single <see cref="StreamContext{TItem}"/> of <typeparamref name="TDocument"/>
/// (fan-in), preserving the change feed's per-partition-key-range ordering and enabling
/// windowing/aggregation. Unlike the opaque-payload transports (Event Hubs, Service Bus, Kafka), the
/// Cosmos DB trigger delivers already-deserialized documents, so the pipeline is generic over the
/// document type rather than fanning out routable message envelopes.
/// </summary>
public static class StreamingExtensions
{
    /// <summary>
    /// Adds a Cosmos DB Change Feed entry point that runs the pipeline once over the whole batch of
    /// changed documents. The Azure Functions <c>CosmosDBTrigger</c> checkpoints its lease
    /// automatically when the invocation returns successfully, so the context's checkpointer is the
    /// no-op default; an exception thrown from the pipeline propagates to the runtime, leaving the
    /// lease untouched for redelivery of the whole batch.
    /// </summary>
    /// <typeparam name="TDocument">The document type the change feed batch is deserialized into.</typeparam>
    /// <param name="app">The Azure Function app builder.</param>
    /// <param name="action">Configures the stream pipeline (add <c>UseStream(...)</c> etc.).</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseCosmosDbChangeFeed<TDocument>(this IAzureFunctionAppBuilder app,
        Action<IMiddlewarePipelineBuilder<StreamContext<TDocument>>> action)
    {
        var pipeline = app.Create<StreamContext<TDocument>>();
        action(pipeline);
        app.Add(serviceResolverFactory => new EntryPointMiddlewareApplication<IReadOnlyList<TDocument>>(
            new StreamMiddlewareApplication<IReadOnlyList<TDocument>, TDocument>(pipeline.Build(), ToStreamContext),
            serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies Cosmos DB Change Feed configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <typeparam name="TDocument">The document type the change feed batch is deserialized into.</typeparam>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">Configures the stream pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseCosmosDbChangeFeed<TDocument>(this IBenzeneApplicationBuilder app,
        Action<IMiddlewarePipelineBuilder<StreamContext<TDocument>>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseCosmosDbChangeFeed(action);
        }

        return app;
    }

    private static StreamContext<TDocument> ToStreamContext<TDocument>(IReadOnlyList<TDocument> documents)
    {
        return new StreamContext<TDocument>(ToAsyncEnumerable(documents ?? Array.Empty<TDocument>()));
    }

    private static async IAsyncEnumerable<TDocument> ToAsyncEnumerable<TDocument>(IReadOnlyList<TDocument> documents)
    {
        foreach (var document in documents)
        {
            yield return document;
        }

        await Task.CompletedTask;
    }
}

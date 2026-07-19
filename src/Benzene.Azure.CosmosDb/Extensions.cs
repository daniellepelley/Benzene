using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Benzene.SelfHost;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Provides extension methods for adding a standalone Cosmos DB Change Feed consumer to a Benzene
/// worker.
/// </summary>
/// <remarks>
/// Unlike <c>Benzene.Azure.Function.CosmosDb</c>, which processes batches delivered via an Azure
/// Functions <c>CosmosDBTrigger</c>, this package consumes the change feed directly using
/// <see cref="BenzeneCosmosChangeFeedWorker{TDocument}"/> - intended for long-running workers
/// (e.g. <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>) rather than Azure Functions, and
/// for handlers that want manual per-batch checkpoint control.
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Registers the services Cosmos DB Change Feed consumption depends on beyond the entry point
    /// application itself - currently just the <see cref="ITransportInfo"/> advertising
    /// <c>"cosmos-db"</c> as a wired transport. Called automatically by
    /// <see cref="UseCosmosDbChangeFeed{TDocument}"/>.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddCosmosDbChangeFeed(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.CosmosDb));
        return services;
    }

    /// <summary>
    /// Adds a Cosmos DB Change Feed consumer to the worker. There is no
    /// <c>UseMessageHandlers()</c>-style routing on this transport - changed documents carry no
    /// message envelope - so the pipeline is a streaming pipeline over the document type,
    /// mirroring the Functions trigger adapter's <c>UseCosmosDbChangeFeed&lt;TDocument&gt;</c>.
    /// </summary>
    /// <typeparam name="TDocument">The document type the change feed batches are deserialized into.</typeparam>
    /// <param name="app">The worker startup to add the change feed consumer to.</param>
    /// <param name="config">The checkpointing and failure-handling behavior to use.</param>
    /// <param name="processorFactory">
    /// The factory used to create the underlying <c>ChangeFeedProcessor</c> (which decides the
    /// monitored container, lease container, processor/instance names, and authentication).
    /// </param>
    /// <param name="action">Configures the stream pipeline (add <c>UseStream(...)</c> etc.).</param>
    /// <returns>The worker startup for method chaining.</returns>
    public static IBenzeneWorkerStartup UseCosmosDbChangeFeed<TDocument>(this IBenzeneWorkerStartup app,
        BenzeneCosmosChangeFeedConfig config, ICosmosChangeFeedProcessorFactory<TDocument> processorFactory,
        Action<IMiddlewarePipelineBuilder<StreamContext<TDocument>>> action)
    {
        app.Register(x => x.AddCosmosDbChangeFeed());
        var middlewarePipelineBuilder = app.Create<StreamContext<TDocument>>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var application = new CosmosChangeFeedApplication<TDocument>(pipeline);
        app.Add(serviceResolverFactory => new BenzeneCosmosChangeFeedWorker<TDocument>(
            serviceResolverFactory, application, config, processorFactory));
        return app;
    }
}

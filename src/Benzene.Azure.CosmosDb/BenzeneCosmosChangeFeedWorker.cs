using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// A long-running worker that consumes a Cosmos DB container's change feed directly via the SDK's
/// Change Feed Processor and runs each delivered batch through a Benzene streaming pipeline - for
/// <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>, not Azure Functions (use
/// <c>Benzene.Azure.Function.CosmosDb</c> for a <c>CosmosDBTrigger</c>).
/// </summary>
/// <remarks>
/// This is one of the "self-hosted worker" startup modes documented in <c>docs/hosting.md</c> -
/// like <c>BenzeneEventHubWorker</c>, nothing is polled by hand: the processor owns lease
/// ownership (one lease per partition key range, stored in a lease container in Cosmos itself),
/// load balancing across worker instances, and in-order batch delivery per lease. What this worker
/// adds over the Functions trigger is <em>manual checkpoint control</em>: each batch's
/// <c>StreamContext&lt;TDocument&gt;</c> carries a real checkpointer wrapping the SDK's batch-level
/// manual checkpoint hook, with auto-checkpoint-on-success as the default
/// (<see cref="BenzeneCosmosChangeFeedConfig.AutoCheckpointOnSuccess"/>) and skip-vs-retry failure
/// semantics (<see cref="BenzeneCosmosChangeFeedConfig.CatchHandlerExceptions"/>).
/// <see cref="StartAsync"/> starts the processor and returns; <see cref="StopAsync"/> stops it,
/// waiting for in-flight batches to finish (the SDK's start/stop take no cancellation token, so
/// the host's tokens are not observed).
/// </remarks>
/// <typeparam name="TDocument">The document type the change feed batches are deserialized into.</typeparam>
public class BenzeneCosmosChangeFeedWorker<TDocument> : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly CosmosChangeFeedApplication<TDocument> _application;
    private readonly BenzeneCosmosChangeFeedConfig _config;
    private readonly ICosmosChangeFeedProcessorFactory<TDocument> _processorFactory;
    private ChangeFeedProcessor? _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneCosmosChangeFeedWorker{TDocument}"/> class.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    /// <param name="application">The application that runs each batch through the streaming pipeline.</param>
    /// <param name="config">The checkpointing and failure-handling behavior to use.</param>
    /// <param name="processorFactory">The factory used to create the underlying processor.</param>
    public BenzeneCosmosChangeFeedWorker(IServiceResolverFactory serviceResolverFactory,
        CosmosChangeFeedApplication<TDocument> application, BenzeneCosmosChangeFeedConfig config,
        ICosmosChangeFeedProcessorFactory<TDocument> processorFactory)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _application = application;
        _config = config;
        _processorFactory = processorFactory;
    }

    /// <summary>
    /// Creates the processor and starts it. Returns once the processor is running - it does not
    /// block until shutdown. Use <see cref="StopAsync"/> to stop consuming and wait for in-flight
    /// batches to finish.
    /// </summary>
    /// <param name="cancellationToken">Unobserved - the SDK's <c>StartAsync</c> takes no token.</param>
    /// <returns>A task that completes when the processor has started.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _processor = _processorFactory.Create(OnChangesAsync, OnErrorAsync);
        await _processor.StartAsync();
    }

    /// <summary>
    /// Stops the processor, waiting for in-flight batch handlers to finish and lease ownership to
    /// be relinquished.
    /// </summary>
    /// <param name="cancellationToken">Unobserved - the SDK's <c>StopAsync</c> takes no token.</param>
    /// <returns>A task that completes when the processor has stopped.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopAsync();
        }
    }

    private async Task OnChangesAsync(ChangeFeedProcessorContext context, IReadOnlyCollection<TDocument> changes,
        Func<Task> checkpointAsync, CancellationToken cancellationToken)
    {
        var batch = new CosmosChangeFeedBatch<TDocument>(changes, checkpointAsync, context.LeaseToken, cancellationToken);

        try
        {
            var handlerCheckpointed = await _application.HandleAsync(batch, _serviceResolverFactory);
            if (!handlerCheckpointed && _config.AutoCheckpointOnSuccess)
            {
                await checkpointAsync();
            }
        }
        catch (Exception ex)
        {
            using (var loggingScope = _serviceResolverFactory.CreateScope())
            {
                loggingScope.GetService<ILogger<BenzeneCosmosChangeFeedWorker<TDocument>>>()
                    .LogError(ex, "Processing change feed batch of {count} documents on lease {leaseToken} failed",
                        changes.Count, context.LeaseToken);
            }

            if (_config.CatchHandlerExceptions)
            {
                // Skip mode: checkpoint the failed batch anyway so it is permanently passed over
                // and the lease keeps moving.
                await checkpointAsync();
            }
            else
            {
                // Retry mode (default): let the exception reach the processor - the lease is not
                // advanced and the same batch is redelivered (at-least-once).
                throw;
            }
        }
    }

    private Task OnErrorAsync(string leaseToken, Exception exception)
    {
        using var loggingScope = _serviceResolverFactory.CreateScope();
        loggingScope.GetService<ILogger<BenzeneCosmosChangeFeedWorker<TDocument>>>()
            .LogError(exception, "Change feed processing failed on lease {leaseToken}", leaseToken);
        return Task.CompletedTask;
    }
}

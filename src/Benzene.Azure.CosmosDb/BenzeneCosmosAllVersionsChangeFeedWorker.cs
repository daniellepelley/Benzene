using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// A long-running worker that consumes a Cosmos DB container's change feed in
/// <em>all-versions-and-deletes</em> mode and runs each delivered batch through a Benzene streaming
/// pipeline of <see cref="CosmosChangeFeedItem{TDocument}"/> (current + previous + change type). The
/// sibling of <see cref="BenzeneCosmosChangeFeedWorker{TDocument}"/> for the mode where deletes and
/// intermediate versions surface (requires caller-configured container/account retention).
/// </summary>
/// <remarks>
/// The SDK's all-versions-and-deletes processor is <em>automatic-checkpoint only</em> - it checkpoints
/// after the change handler returns successfully - so, unlike the manual-checkpoint sibling, there is
/// no per-batch checkpointer and no auto-checkpoint knob. The only failure decision is
/// <see cref="BenzeneCosmosAllVersionsChangeFeedConfig.CatchHandlerExceptions"/>: swallow (checkpoint
/// advances, poison batch skipped) or rethrow (no checkpoint, batch redelivered - the default,
/// at-least-once).
/// </remarks>
/// <typeparam name="TDocument">The document type the change items are deserialized into.</typeparam>
public class BenzeneCosmosAllVersionsChangeFeedWorker<TDocument> : IBenzeneWorker
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly CosmosAllVersionsChangeFeedApplication<TDocument> _application;
    private readonly BenzeneCosmosAllVersionsChangeFeedConfig _config;
    private readonly ICosmosChangeFeedProcessorFactory<TDocument> _processorFactory;
    private ChangeFeedProcessor? _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneCosmosAllVersionsChangeFeedWorker{TDocument}"/> class.
    /// </summary>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    /// <param name="application">The application that runs each batch through the streaming pipeline.</param>
    /// <param name="config">The failure-handling behavior to use.</param>
    /// <param name="processorFactory">The factory used to create the underlying all-versions processor.</param>
    public BenzeneCosmosAllVersionsChangeFeedWorker(IServiceResolverFactory serviceResolverFactory,
        CosmosAllVersionsChangeFeedApplication<TDocument> application, BenzeneCosmosAllVersionsChangeFeedConfig config,
        ICosmosChangeFeedProcessorFactory<TDocument> processorFactory)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _application = application;
        _config = config;
        _processorFactory = processorFactory;
    }

    /// <summary>Creates the all-versions processor and starts it, returning once it is running.</summary>
    /// <param name="cancellationToken">Unobserved - the SDK's <c>StartAsync</c> takes no token.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _processor = _processorFactory.CreateAllVersionsAndDeletes(OnChangesAsync, OnErrorAsync);
        await _processor.StartAsync();
    }

    /// <summary>Stops the processor, waiting for in-flight batch handlers to finish.</summary>
    /// <param name="cancellationToken">Unobserved - the SDK's <c>StopAsync</c> takes no token.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopAsync();
        }
    }

    private async Task OnChangesAsync(ChangeFeedProcessorContext context,
        IReadOnlyCollection<ChangeFeedItem<TDocument>> changes, CancellationToken cancellationToken)
    {
        try
        {
            // Map inside the try: a mapping failure is a batch that can never succeed, so under skip mode
            // it must still be checkpointed-and-skipped rather than escaping and redelivering forever.
            var items = changes.Select(Map).ToArray();
            var batch = new CosmosAllVersionsChangeFeedBatch<TDocument>(items, context.LeaseToken, cancellationToken);
            await _application.HandleAsync(batch, _serviceResolverFactory, cancellationToken);
            // Automatic-checkpoint mode: returning here lets the processor checkpoint this batch.
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown cancelled the batch mid-flight. Even in skip mode, do NOT swallow this: returning
            // normally would let the automatic-checkpoint processor checkpoint a partially-processed
            // batch, silently losing the unprocessed tail. Propagate so the batch is redelivered.
            throw;
        }
        catch (Exception ex)
        {
            using (var loggingScope = _serviceResolverFactory.CreateScope())
            {
                loggingScope.GetService<ILogger<BenzeneCosmosAllVersionsChangeFeedWorker<TDocument>>>()
                    .LogError(ex, "Processing all-versions change feed batch of {count} changes on lease {leaseToken} failed",
                        changes.Count, context.LeaseToken);
            }

            if (!_config.CatchHandlerExceptions)
            {
                // Retry mode (default): let the exception reach the processor - it does not checkpoint,
                // so the same batch is redelivered (at-least-once).
                throw;
            }

            // Skip mode: swallow so the processor checkpoints and the poison batch is passed over.
        }
    }

    private static CosmosChangeFeedItem<TDocument> Map(ChangeFeedItem<TDocument> item)
    {
        return new CosmosChangeFeedItem<TDocument>(item.Current, item.Previous, MapChangeType(item.Metadata.OperationType));
    }

    private static CosmosChangeType MapChangeType(ChangeFeedOperationType operationType) => operationType switch
    {
        ChangeFeedOperationType.Create => CosmosChangeType.Create,
        ChangeFeedOperationType.Delete => CosmosChangeType.Delete,
        _ => CosmosChangeType.Replace,
    };

    private Task OnErrorAsync(string leaseToken, Exception exception)
    {
        using var loggingScope = _serviceResolverFactory.CreateScope();
        loggingScope.GetService<ILogger<BenzeneCosmosAllVersionsChangeFeedWorker<TDocument>>>()
            .LogError(exception, "All-versions change feed processing failed on lease {leaseToken}", leaseToken);
        return Task.CompletedTask;
    }
}

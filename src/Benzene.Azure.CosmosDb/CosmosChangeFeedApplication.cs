using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Runs a change feed batch through the streaming pipeline as a single
/// <see cref="StreamContext{TItem}"/> (fan-in): the whole batch is exposed as one ordered
/// <see cref="IAsyncEnumerable{T}"/> of documents, processed by one pipeline run in one DI scope -
/// the same shape as <c>Benzene.Azure.Function.CosmosDb</c>'s trigger adapter and AWS's
/// <c>KinesisStreamApplication</c>. The context is wired with a real batch-level checkpointer
/// (wrapping the SDK's manual checkpoint hook), and <c>HandleAsync</c> returns whether the handler
/// called it, so <see cref="BenzeneCosmosChangeFeedWorker{TDocument}"/> can decide auto-checkpoint
/// behavior. Unlike Kinesis, a pipeline exception is <em>not</em> caught here - the worker owns
/// the catch/skip/retry decision.
/// </summary>
/// <typeparam name="TDocument">The document type the change feed batches are deserialized into.</typeparam>
public class CosmosChangeFeedApplication<TDocument>
    : StreamMiddlewareApplication<CosmosChangeFeedBatch<TDocument>, TDocument, bool>
{
    /// <summary>
    /// The <see cref="StreamContext{TItem}.Metadata"/> key holding the batch's lease token (the
    /// partition key range the batch was read from).
    /// </summary>
    public const string LeaseTokenMetadataKey = "cosmosDb.leaseToken";

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosChangeFeedApplication{TDocument}"/> class.
    /// </summary>
    /// <param name="pipeline">The built stream pipeline to run each batch through.</param>
    public CosmosChangeFeedApplication(IMiddlewarePipeline<StreamContext<TDocument>> pipeline)
        : base(
            new TransportMiddlewarePipeline<StreamContext<TDocument>>(TransportNames.CosmosDb, pipeline),
            BuildContext,
            context => ((CosmosChangeFeedStreamCheckpointer<TDocument>)context.Checkpointer).HasCheckpointed)
    { }

    private static StreamContext<TDocument> BuildContext(CosmosChangeFeedBatch<TDocument> batch)
    {
        return new StreamContext<TDocument>(
            ToAsyncEnumerable(batch.Changes),
            checkpointer: new CosmosChangeFeedStreamCheckpointer<TDocument>(batch.CheckpointAsync),
            cancellationToken: batch.CancellationToken,
            metadata: new Dictionary<string, object> { [LeaseTokenMetadataKey] = batch.LeaseToken });
    }

    private static async IAsyncEnumerable<TDocument> ToAsyncEnumerable(IReadOnlyCollection<TDocument> documents)
    {
        foreach (var document in documents)
        {
            yield return document;
        }

        await Task.CompletedTask;
    }
}

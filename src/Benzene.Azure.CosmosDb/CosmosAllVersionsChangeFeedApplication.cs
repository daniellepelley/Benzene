using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Runs an all-versions-and-deletes change feed batch through the streaming pipeline as a single
/// <see cref="StreamContext{TItem}"/> of <see cref="CosmosChangeFeedItem{TDocument}"/> (fan-in) - the
/// same shape as <see cref="CosmosChangeFeedApplication{TDocument}"/> but over change items (current +
/// previous + change type) rather than bare documents, and with <em>no</em> checkpointer: the
/// all-versions-and-deletes processor is automatic-checkpoint only, so the context uses the default
/// <c>NullStreamCheckpointer</c> and progress is advanced by the handler returning without throwing.
/// </summary>
/// <typeparam name="TDocument">The document type the change items are deserialized into.</typeparam>
public class CosmosAllVersionsChangeFeedApplication<TDocument>
    : StreamMiddlewareApplication<CosmosAllVersionsChangeFeedBatch<TDocument>, CosmosChangeFeedItem<TDocument>>
{
    /// <summary>
    /// The <see cref="StreamContext{TItem}.Metadata"/> key holding the batch's lease token (the
    /// partition key range the batch was read from).
    /// </summary>
    public const string LeaseTokenMetadataKey = "cosmosDb.leaseToken";

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosAllVersionsChangeFeedApplication{TDocument}"/> class.
    /// </summary>
    /// <param name="pipeline">The built stream pipeline to run each batch through.</param>
    public CosmosAllVersionsChangeFeedApplication(IMiddlewarePipeline<StreamContext<CosmosChangeFeedItem<TDocument>>> pipeline)
        : base(
            new TransportMiddlewarePipeline<StreamContext<CosmosChangeFeedItem<TDocument>>>(TransportNames.CosmosDb, pipeline),
            BuildContext)
    { }

    private static StreamContext<CosmosChangeFeedItem<TDocument>> BuildContext(CosmosAllVersionsChangeFeedBatch<TDocument> batch)
    {
        return new StreamContext<CosmosChangeFeedItem<TDocument>>(
            ToAsyncEnumerable(batch.Changes),
            checkpointer: null, // automatic-checkpoint mode: NullStreamCheckpointer, progress rides on success
            cancellationToken: batch.CancellationToken,
            metadata: new Dictionary<string, object> { [LeaseTokenMetadataKey] = batch.LeaseToken });
    }

    private static async IAsyncEnumerable<CosmosChangeFeedItem<TDocument>> ToAsyncEnumerable(
        IReadOnlyCollection<CosmosChangeFeedItem<TDocument>> changes)
    {
        foreach (var change in changes)
        {
            yield return change;
        }

        await Task.CompletedTask;
    }
}

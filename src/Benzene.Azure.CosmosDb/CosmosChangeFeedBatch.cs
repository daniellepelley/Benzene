namespace Benzene.Azure.CosmosDb;

/// <summary>
/// One delivered change feed batch: the raw event <see cref="CosmosChangeFeedApplication{TDocument}"/>
/// maps into a <c>StreamContext&lt;TDocument&gt;</c>. Carries the batch's documents together with the
/// SDK's batch-level manual checkpoint hook and lease identity - the change feed has no per-document
/// resume token, so <see cref="CheckpointAsync"/> acknowledges the whole batch as a unit.
/// </summary>
/// <typeparam name="TDocument">The document type the batch was deserialized into.</typeparam>
public class CosmosChangeFeedBatch<TDocument>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosChangeFeedBatch{TDocument}"/> class.
    /// </summary>
    /// <param name="changes">The changed documents, in change feed order for the lease's partition key range.</param>
    /// <param name="checkpointAsync">The SDK's batch-level checkpoint hook for this delivery.</param>
    /// <param name="leaseToken">The lease (partition key range) the batch was read from.</param>
    /// <param name="cancellationToken">The SDK's cancellation token for this delivery.</param>
    public CosmosChangeFeedBatch(IReadOnlyCollection<TDocument> changes, Func<Task> checkpointAsync,
        string leaseToken, CancellationToken cancellationToken)
    {
        Changes = changes;
        CheckpointAsync = checkpointAsync;
        LeaseToken = leaseToken;
        CancellationToken = cancellationToken;
    }

    /// <summary>The changed documents, in change feed order for the lease's partition key range.</summary>
    public IReadOnlyCollection<TDocument> Changes { get; }

    /// <summary>The SDK's batch-level checkpoint hook for this delivery.</summary>
    public Func<Task> CheckpointAsync { get; }

    /// <summary>The lease (partition key range) the batch was read from.</summary>
    public string LeaseToken { get; }

    /// <summary>The SDK's cancellation token for this delivery.</summary>
    public CancellationToken CancellationToken { get; }
}

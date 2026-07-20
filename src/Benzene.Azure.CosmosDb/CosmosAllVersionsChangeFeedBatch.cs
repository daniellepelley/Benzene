namespace Benzene.Azure.CosmosDb;

/// <summary>
/// One delivered all-versions-and-deletes change feed batch: the changes (each a
/// <see cref="CosmosChangeFeedItem{TDocument}"/> carrying current/previous state and change type),
/// plus the lease identity and cancellation token. Unlike <see cref="CosmosChangeFeedBatch{TDocument}"/>,
/// there is no manual checkpoint hook - the SDK's all-versions-and-deletes builder is automatic-
/// checkpoint only (it checkpoints after the handler returns successfully), so progress is advanced by
/// the handler completing without throwing rather than by an explicit call.
/// </summary>
/// <typeparam name="TDocument">The document type the changes were deserialized into.</typeparam>
public class CosmosAllVersionsChangeFeedBatch<TDocument>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosAllVersionsChangeFeedBatch{TDocument}"/> class.
    /// </summary>
    /// <param name="changes">The changes, in change feed order for the lease's partition key range.</param>
    /// <param name="leaseToken">The lease (partition key range) the batch was read from.</param>
    /// <param name="cancellationToken">The SDK's cancellation token for this delivery.</param>
    public CosmosAllVersionsChangeFeedBatch(IReadOnlyCollection<CosmosChangeFeedItem<TDocument>> changes,
        string leaseToken, CancellationToken cancellationToken)
    {
        Changes = changes;
        LeaseToken = leaseToken;
        CancellationToken = cancellationToken;
    }

    /// <summary>The changes, in change feed order for the lease's partition key range.</summary>
    public IReadOnlyCollection<CosmosChangeFeedItem<TDocument>> Changes { get; }

    /// <summary>The lease (partition key range) the batch was read from.</summary>
    public string LeaseToken { get; }

    /// <summary>The SDK's cancellation token for this delivery.</summary>
    public CancellationToken CancellationToken { get; }
}

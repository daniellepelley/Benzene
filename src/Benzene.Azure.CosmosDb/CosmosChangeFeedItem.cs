namespace Benzene.Azure.CosmosDb;

/// <summary>
/// One change surfaced by the change feed's <em>all-versions-and-deletes</em> mode: the document's
/// state after the change (<see cref="Current"/>), its state before (<see cref="Previous"/>, when the
/// account/container retention captures it), and the <see cref="ChangeType"/>. A Benzene-owned
/// projection of the SDK's <c>ChangeFeedItem&lt;T&gt;</c> so handlers stream a plain Benzene type.
/// </summary>
/// <remarks>
/// For a <see cref="CosmosChangeType.Delete"/>, <see cref="Current"/> is typically the tombstone
/// (id/partition-key only or default) and the meaningful prior state, if retained, is in
/// <see cref="Previous"/>. For <see cref="CosmosChangeType.Create"/>/<see cref="CosmosChangeType.Replace"/>,
/// <see cref="Current"/> is the changed document. All-versions-and-deletes requires the caller to have
/// configured container/account retention; without it, deletes and intermediate versions don't surface.
/// </remarks>
/// <typeparam name="TDocument">The document type the change feed items are deserialized into.</typeparam>
public class CosmosChangeFeedItem<TDocument>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosChangeFeedItem{TDocument}"/> class.
    /// </summary>
    /// <param name="current">The document's state after the change.</param>
    /// <param name="previous">The document's state before the change, if retained; otherwise default.</param>
    /// <param name="changeType">The kind of change.</param>
    public CosmosChangeFeedItem(TDocument current, TDocument previous, CosmosChangeType changeType)
    {
        Current = current;
        Previous = previous;
        ChangeType = changeType;
    }

    /// <summary>The document's state after the change (the tombstone for a delete).</summary>
    public TDocument Current { get; }

    /// <summary>The document's state before the change, when retention captured it; otherwise default.</summary>
    public TDocument Previous { get; }

    /// <summary>The kind of change (create, replace, or delete).</summary>
    public CosmosChangeType ChangeType { get; }
}

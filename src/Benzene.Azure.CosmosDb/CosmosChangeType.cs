namespace Benzene.Azure.CosmosDb;

/// <summary>
/// The kind of change a document underwent, as surfaced by the change feed's
/// <em>all-versions-and-deletes</em> mode. A Benzene-owned projection of the Cosmos SDK's
/// <c>ChangeFeedOperationType</c>, so change handlers don't take a direct dependency on the SDK enum.
/// </summary>
public enum CosmosChangeType
{
    /// <summary>The document was created.</summary>
    Create,

    /// <summary>The document was replaced/updated.</summary>
    Replace,

    /// <summary>The document was deleted (only surfaced in all-versions-and-deletes mode).</summary>
    Delete,
}

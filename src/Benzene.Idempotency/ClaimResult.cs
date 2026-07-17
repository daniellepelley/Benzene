namespace Benzene.Idempotency;

/// <summary>
/// The outcome of an <see cref="IIdempotencyStore.TryClaimAsync"/> call: either this caller won the
/// claim and should process the message, or a record already existed (a duplicate).
/// </summary>
public class ClaimResult
{
    private ClaimResult(bool claimed, IdempotencyRecord? existingRecord)
    {
        Claimed = claimed;
        ExistingRecord = existingRecord;
    }

    /// <summary>
    /// Gets whether this caller won the claim. When <c>true</c>, the caller is the first to see this
    /// key and should process the message; when <c>false</c>, the message is a duplicate.
    /// </summary>
    public bool Claimed { get; }

    /// <summary>
    /// Gets the record that already existed when the claim was refused. <c>null</c> when
    /// <see cref="Claimed"/> is <c>true</c>.
    /// </summary>
    public IdempotencyRecord? ExistingRecord { get; }

    /// <summary>Creates a result indicating the caller won the claim.</summary>
    public static ClaimResult Won() => new(true, null);

    /// <summary>Creates a result indicating a record already existed (the message is a duplicate).</summary>
    /// <param name="existing">The record already present in the store.</param>
    public static ClaimResult AlreadyExists(IdempotencyRecord existing) => new(false, existing);
}

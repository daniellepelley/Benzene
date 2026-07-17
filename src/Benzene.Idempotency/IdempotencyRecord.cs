namespace Benzene.Idempotency;

/// <summary>
/// The persisted record for one idempotency key: whether it is still in progress or completed, and
/// (once completed) whether the first processing attempt succeeded.
/// </summary>
public class IdempotencyRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyRecord"/> class.
    /// </summary>
    /// <param name="key">The idempotency key this record is for.</param>
    /// <param name="status">The record's lifecycle state.</param>
    /// <param name="wasSuccessful">
    /// Whether the first processing attempt succeeded. Only meaningful when <paramref name="status"/>
    /// is <see cref="IdempotencyStatus.Completed"/>.
    /// </param>
    public IdempotencyRecord(string key, IdempotencyStatus status, bool wasSuccessful = false)
    {
        Key = key;
        Status = status;
        WasSuccessful = wasSuccessful;
    }

    /// <summary>Gets the idempotency key this record is for.</summary>
    public string Key { get; }

    /// <summary>Gets the record's lifecycle state.</summary>
    public IdempotencyStatus Status { get; }

    /// <summary>
    /// Gets whether the first processing attempt succeeded. Only meaningful when <see cref="Status"/>
    /// is <see cref="IdempotencyStatus.Completed"/>.
    /// </summary>
    public bool WasSuccessful { get; }
}

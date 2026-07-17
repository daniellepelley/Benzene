namespace Benzene.Idempotency;

/// <summary>
/// Lifecycle state of an idempotency record in an <see cref="IIdempotencyStore"/>.
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>
    /// The key has been claimed and the message is currently being processed (or the processing
    /// instance crashed without releasing it, in which case the record lingers until it expires).
    /// </summary>
    InProgress,

    /// <summary>
    /// Processing finished and the outcome has been recorded. Subsequent redeliveries of the same
    /// key are duplicates and are short-circuited.
    /// </summary>
    Completed
}

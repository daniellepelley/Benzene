namespace Benzene.Idempotency;

/// <summary>
/// Thrown when a duplicate message arrives while the first copy is still in progress and
/// <see cref="IdempotencyOptions.InProgressBehavior"/> is <see cref="InProgressBehavior.Throw"/>.
/// The transport should not acknowledge the message, so it is redelivered later.
/// </summary>
public class IdempotencyConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyConflictException"/> class.
    /// </summary>
    /// <param name="key">The idempotency key that is already being processed.</param>
    public IdempotencyConflictException(string key)
        : base($"A message with idempotency key '{key}' is already being processed.")
    {
        Key = key;
    }

    /// <summary>Gets the idempotency key that is already being processed.</summary>
    public string Key { get; }
}

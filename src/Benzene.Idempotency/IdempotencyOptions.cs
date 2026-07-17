namespace Benzene.Idempotency;

/// <summary>
/// Configuration for the idempotency middleware and the default key strategy.
/// </summary>
public class IdempotencyOptions
{
    /// <summary>
    /// The message header carrying a caller-supplied idempotency key. Defaults to
    /// <c>idempotency-key</c>. When a message carries this header, its value is used as the key.
    /// </summary>
    public string HeaderName { get; set; } = IdempotencyDefaults.HeaderName;

    /// <summary>
    /// When a message has no idempotency-key header, whether to derive a key by hashing the message
    /// topic and body. Defaults to <c>true</c>. Set to <c>false</c> to only de-duplicate messages
    /// that carry an explicit key and let everything else through untracked.
    /// </summary>
    public bool HashBodyWhenNoHeader { get; set; } = true;

    /// <summary>
    /// An optional prefix applied to every key, for namespacing when several services share one
    /// store. Defaults to an empty string.
    /// </summary>
    public string KeyPrefix { get; set; } = "";

    /// <summary>
    /// How a duplicate that arrives while the first copy is still in progress is handled. Defaults
    /// to <see cref="InProgressBehavior.Skip"/>.
    /// </summary>
    public InProgressBehavior InProgressBehavior { get; set; } = InProgressBehavior.Skip;
}

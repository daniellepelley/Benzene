namespace Benzene.Idempotency;

/// <summary>
/// Pluggable persistence for idempotency keys. Records which messages have already been (or are
/// currently being) processed so that redeliveries on an at-least-once transport can be
/// de-duplicated. Swap the implementation to change where records live (in-memory for a single
/// instance, Redis/a database for a multi-instance deployment) without touching the middleware.
/// </summary>
/// <remarks>
/// The store owns its own retention policy (time-to-live); the middleware never passes an expiry.
/// Keep records long enough to outlive the transport's maximum redelivery window.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically claims <paramref name="key"/> for first-time processing.
    /// <list type="bullet">
    /// <item>If no live record exists, persists a new <see cref="IdempotencyStatus.InProgress"/>
    /// record and returns <see cref="ClaimResult.Won"/>.</item>
    /// <item>If a live record already exists, returns
    /// <see cref="ClaimResult.AlreadyExists"/> with that record and leaves it unchanged.</item>
    /// </list>
    /// Implementations MUST make the check-and-insert atomic (e.g. Redis <c>SET key val NX</c>, a
    /// unique-key insert) so concurrent redeliveries cannot both win the claim.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<ClaimResult> TryClaimAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes a previously-claimed key to <see cref="IdempotencyStatus.Completed"/>, recording the
    /// outcome so future duplicates can be short-circuited.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="wasSuccessful">Whether the first processing attempt succeeded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task CompleteAsync(string key, bool wasSuccessful, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a claim so the message can be reprocessed when the transport redelivers it. Called
    /// when the handler throws or reports failure, so a transient error does not permanently
    /// suppress the message.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ReleaseAsync(string key, CancellationToken cancellationToken = default);
}

namespace Benzene.Idempotency;

/// <summary>
/// Derives the idempotency key for a message from its transport context. Register a custom
/// implementation to key de-duplication on something other than the header/body default (e.g. a
/// business identifier pulled from the payload).
/// </summary>
/// <typeparam name="TContext">The transport-specific message context type.</typeparam>
public interface IIdempotencyKeyStrategy<in TContext>
{
    /// <summary>
    /// Returns the idempotency key for the message, or <c>null</c> to skip de-duplication and let
    /// the message through untracked.
    /// </summary>
    /// <param name="context">The transport-specific message context.</param>
    string? GetKey(TContext context);
}

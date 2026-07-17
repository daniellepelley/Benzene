namespace Benzene.Idempotency;

/// <summary>
/// How the middleware treats a duplicate that arrives while the first copy is still
/// <see cref="IdempotencyStatus.InProgress"/> (a genuinely concurrent redelivery, or a record left
/// behind by an instance that crashed mid-processing).
/// </summary>
public enum InProgressBehavior
{
    /// <summary>
    /// Drop the duplicate without invoking the handler and let the transport acknowledge it. The
    /// first copy is expected to complete (or release on failure). This is the default: it never
    /// double-processes, at the cost of dropping the duplicate if the first copy ultimately fails
    /// before releasing its claim.
    /// </summary>
    Skip,

    /// <summary>
    /// Throw <see cref="IdempotencyConflictException"/> so the transport does not acknowledge the
    /// duplicate and redelivers it later, by which time the first copy has usually finished. Use
    /// this when losing a duplicate whose sibling later fails is unacceptable.
    /// </summary>
    Throw
}

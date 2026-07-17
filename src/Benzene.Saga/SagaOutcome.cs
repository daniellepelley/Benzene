namespace Benzene.Saga;

/// <summary>
/// The overall outcome of running a <see cref="Saga"/>.
/// </summary>
public enum SagaOutcome
{
    /// <summary>Every stage completed successfully.</summary>
    Succeeded,

    /// <summary>
    /// A stage failed and every effect created by earlier (and concurrently-succeeded) steps was
    /// successfully compensated - the system is back to its starting state and the saga can be retried.
    /// </summary>
    RolledBack,

    /// <summary>
    /// A stage failed and rollback ran, but at least one compensation itself failed - the system may
    /// be left with orphaned effects that need manual attention. See
    /// <see cref="SagaResult.CompensationFailures"/>.
    /// </summary>
    PartiallyRolledBack,
}

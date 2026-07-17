namespace Benzene.Saga;

/// <summary>
/// The lifecycle state of a single <see cref="ISagaStep"/> within a saga run.
/// </summary>
public enum SagaStepState
{
    /// <summary>The step has not run yet.</summary>
    Pending,

    /// <summary>The step's forward action ran and returned a successful result.</summary>
    Succeeded,

    /// <summary>The step's forward action ran and returned a failed result (or threw).</summary>
    Failed,

    /// <summary>The step had succeeded and its compensation ran successfully during rollback.</summary>
    RolledBack,

    /// <summary>
    /// The step had succeeded but its compensation itself failed (or threw) during rollback -
    /// the effect this step created may still exist and needs attention.
    /// </summary>
    CompensationFailed,
}

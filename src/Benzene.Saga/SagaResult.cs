using Benzene.Abstractions.Results;

namespace Benzene.Saga;

/// <summary>
/// The outcome of running a <see cref="Saga"/>: whether it succeeded, and if not, which stage
/// failed, why, and whether rollback was clean.
/// </summary>
public class SagaResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SagaResult"/> class.
    /// </summary>
    /// <param name="outcome">The overall outcome.</param>
    /// <param name="failedStageIndex">The zero-based index of the stage that failed, or <c>null</c> on success.</param>
    /// <param name="failure">The failing step's result, or <c>null</c> on success.</param>
    /// <param name="failureException">The exception the failing step threw, if any.</param>
    /// <param name="compensationFailures">Steps whose compensation itself failed during rollback.</param>
    public SagaResult(SagaOutcome outcome, int? failedStageIndex, IBenzeneResult? failure,
        Exception? failureException, IReadOnlyList<ISagaStep> compensationFailures)
    {
        Outcome = outcome;
        FailedStageIndex = failedStageIndex;
        Failure = failure;
        FailureException = failureException;
        CompensationFailures = compensationFailures;
    }

    /// <summary>Gets the overall outcome.</summary>
    public SagaOutcome Outcome { get; }

    /// <summary>Gets whether the saga completed successfully.</summary>
    public bool IsSuccess => Outcome == SagaOutcome.Succeeded;

    /// <summary>Gets the zero-based index of the stage that failed, or <c>null</c> if the saga succeeded.</summary>
    public int? FailedStageIndex { get; }

    /// <summary>Gets the failing step's result, or <c>null</c> if the saga succeeded.</summary>
    public IBenzeneResult? Failure { get; }

    /// <summary>Gets the exception the failing step threw, if it threw rather than returning a failed result.</summary>
    public Exception? FailureException { get; }

    /// <summary>
    /// Gets the steps whose compensation itself failed during rollback - non-empty only when
    /// <see cref="Outcome"/> is <see cref="SagaOutcome.PartiallyRolledBack"/>. Their effects may
    /// still exist and need manual attention.
    /// </summary>
    public IReadOnlyList<ISagaStep> CompensationFailures { get; }
}

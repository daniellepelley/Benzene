using Benzene.Abstractions.Results;

namespace Benzene.Saga;

/// <summary>
/// A single unit of work in a saga: a forward action paired with an optional compensation that
/// undoes it. Steps are grouped into stages (run concurrently) which are grouped into a
/// <see cref="Saga"/> (run in order). The non-generic surface is what the engine operates on; the
/// result type is captured by <see cref="SagaStep{T}"/>.
/// </summary>
public interface ISagaStep
{
    /// <summary>Gets the current lifecycle state of this step.</summary>
    SagaStepState State { get; }

    /// <summary>
    /// Gets the forward action's result once it has run, or <c>null</c> before it runs. Used for
    /// failure reporting.
    /// </summary>
    IBenzeneResult? Result { get; }

    /// <summary>
    /// Gets the exception the forward action threw, if it threw rather than returning a failed
    /// result; otherwise <c>null</c>.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Runs the forward action, reading any earlier-stage values it needs from <paramref name="context"/>,
    /// and records the outcome onto <see cref="State"/>/<see cref="Result"/>. Does not publish its
    /// own result to the context - that happens via <see cref="Publish"/> once the whole stage
    /// succeeds.
    /// </summary>
    /// <param name="context">The saga context.</param>
    /// <returns>A task that completes when the forward action has run.</returns>
    Task ExecuteAsync(SagaContext context);

    /// <summary>
    /// Publishes this step's successful result into <paramref name="context"/> so later stages can
    /// read it. Called only after the step's stage has fully succeeded.
    /// </summary>
    /// <param name="context">The saga context.</param>
    void Publish(SagaContext context);

    /// <summary>
    /// Compensates this step during rollback. A no-op that reports success if the step did not
    /// succeed or has no compensation.
    /// </summary>
    /// <param name="context">The saga context.</param>
    /// <returns><c>true</c> if there was nothing to undo or the compensation succeeded; <c>false</c> if the compensation itself failed.</returns>
    Task<bool> CompensateAsync(SagaContext context);
}

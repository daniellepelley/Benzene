namespace Benzene.Saga;

/// <summary>
/// A pluggable sink for a saga's progress and outcome, so a crashed or rolled-back saga's last known
/// state is durably recorded — most importantly a <see cref="SagaOutcome.PartiallyRolledBack"/>
/// outcome, whose orphaned effects need an operator to see them.
/// </summary>
/// <remarks>
/// This records progress; it does not <em>resume</em> a saga. The engine's steps are in-process
/// delegates (closures), which can't be serialized and re-hydrated after a crash — durable resume of
/// arbitrary steps isn't something a Func-based engine can offer. What a store gives you is durable
/// observability and operational recovery: what ran, how far it got, and how it ended. The engine
/// calls these methods in order — <see cref="RecordStartedAsync"/>, then
/// <see cref="RecordStageCompletedAsync"/> per completed stage, then <see cref="RecordFinishedAsync"/>
/// — once per attempt. It never enables a store by default; pass one via <see cref="SagaRunOptions"/>.
/// </remarks>
public interface ISagaStateStore
{
    /// <summary>Records that a saga run attempt has started.</summary>
    /// <param name="run">The run's identifying details.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RecordStartedAsync(SagaRunInfo run, CancellationToken cancellationToken = default);

    /// <summary>Records that a stage completed successfully within an attempt.</summary>
    /// <param name="sagaId">The saga instance id.</param>
    /// <param name="attempt">The 1-based attempt number.</param>
    /// <param name="stageIndex">The zero-based index of the completed stage.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RecordStageCompletedAsync(string sagaId, int attempt, int stageIndex, CancellationToken cancellationToken = default);

    /// <summary>Records the final outcome of an attempt.</summary>
    /// <param name="sagaId">The saga instance id.</param>
    /// <param name="attempt">The 1-based attempt number.</param>
    /// <param name="result">The attempt's result.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RecordFinishedAsync(string sagaId, int attempt, SagaResult result, CancellationToken cancellationToken = default);
}

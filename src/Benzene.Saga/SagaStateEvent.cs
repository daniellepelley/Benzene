namespace Benzene.Saga;

/// <summary>The kind of progress event recorded to an <see cref="ISagaStateStore"/>.</summary>
public enum SagaStateEventKind
{
    /// <summary>A saga run attempt started.</summary>
    Started,

    /// <summary>A stage completed successfully within an attempt.</summary>
    StageCompleted,

    /// <summary>An attempt finished (with its <see cref="SagaResult"/>).</summary>
    Finished
}

/// <summary>
/// One recorded saga-progress event, as accumulated by <see cref="InMemorySagaStateStore"/>. A
/// durable store adapter typically persists the same fields to a row/document instead.
/// </summary>
public class SagaStateEvent
{
    /// <summary>Initializes a progress event.</summary>
    /// <param name="sagaId">The saga instance id.</param>
    /// <param name="attempt">The 1-based attempt number.</param>
    /// <param name="kind">The event kind.</param>
    /// <param name="stageIndex">The completed stage index, for a <see cref="SagaStateEventKind.StageCompleted"/> event.</param>
    /// <param name="result">The attempt's result, for a <see cref="SagaStateEventKind.Finished"/> event.</param>
    public SagaStateEvent(string sagaId, int attempt, SagaStateEventKind kind, int? stageIndex = null, SagaResult? result = null)
    {
        SagaId = sagaId;
        Attempt = attempt;
        Kind = kind;
        StageIndex = stageIndex;
        Result = result;
    }

    /// <summary>Gets the saga instance id.</summary>
    public string SagaId { get; }

    /// <summary>Gets the 1-based attempt number.</summary>
    public int Attempt { get; }

    /// <summary>Gets the event kind.</summary>
    public SagaStateEventKind Kind { get; }

    /// <summary>Gets the completed stage index (only for <see cref="SagaStateEventKind.StageCompleted"/>).</summary>
    public int? StageIndex { get; }

    /// <summary>Gets the attempt's result (only for <see cref="SagaStateEventKind.Finished"/>).</summary>
    public SagaResult? Result { get; }
}

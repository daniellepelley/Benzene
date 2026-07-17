namespace Benzene.Saga;

/// <summary>Identifying details of one saga run attempt, passed to an <see cref="ISagaStateStore"/> when the attempt starts.</summary>
public class SagaRunInfo
{
    /// <summary>Initializes the run info.</summary>
    /// <param name="sagaId">The saga instance id (stable across a run's retry attempts).</param>
    /// <param name="name">A human-readable saga name, for grouping/reporting.</param>
    /// <param name="attempt">The 1-based attempt number.</param>
    /// <param name="stageCount">The number of stages in the saga.</param>
    public SagaRunInfo(string sagaId, string name, int attempt, int stageCount)
    {
        SagaId = sagaId;
        Name = name;
        Attempt = attempt;
        StageCount = stageCount;
    }

    /// <summary>Gets the saga instance id.</summary>
    public string SagaId { get; }

    /// <summary>Gets the human-readable saga name.</summary>
    public string Name { get; }

    /// <summary>Gets the 1-based attempt number.</summary>
    public int Attempt { get; }

    /// <summary>Gets the number of stages in the saga.</summary>
    public int StageCount { get; }
}

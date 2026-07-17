namespace Benzene.Saga;

/// <summary>
/// An in-process <see cref="ISagaStateStore"/> that accumulates progress events in a list — for
/// tests, local development, and inspecting what a saga did. A production deployment supplies a
/// durable store (a table/document write) so a rolled-back or partially-rolled-back outcome survives
/// a restart; see <c>docs/cookbooks/sagas.md</c> for a copy-paste durable adapter.
/// </summary>
public class InMemorySagaStateStore : ISagaStateStore
{
    private readonly object _gate = new();
    private readonly List<SagaStateEvent> _events = new();

    /// <summary>Gets a snapshot of every recorded event, in order.</summary>
    public IReadOnlyList<SagaStateEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return _events.ToArray();
            }
        }
    }

    /// <summary>Gets the recorded events for one saga instance, in order.</summary>
    /// <param name="sagaId">The saga instance id.</param>
    public IReadOnlyList<SagaStateEvent> EventsFor(string sagaId)
    {
        lock (_gate)
        {
            return _events.Where(e => e.SagaId == sagaId).ToArray();
        }
    }

    /// <inheritdoc />
    public Task RecordStartedAsync(SagaRunInfo run, CancellationToken cancellationToken = default)
        => Add(new SagaStateEvent(run.SagaId, run.Attempt, SagaStateEventKind.Started));

    /// <inheritdoc />
    public Task RecordStageCompletedAsync(string sagaId, int attempt, int stageIndex, CancellationToken cancellationToken = default)
        => Add(new SagaStateEvent(sagaId, attempt, SagaStateEventKind.StageCompleted, stageIndex));

    /// <inheritdoc />
    public Task RecordFinishedAsync(string sagaId, int attempt, SagaResult result, CancellationToken cancellationToken = default)
        => Add(new SagaStateEvent(sagaId, attempt, SagaStateEventKind.Finished, result: result));

    private Task Add(SagaStateEvent stateEvent)
    {
        lock (_gate)
        {
            _events.Add(stateEvent);
        }

        return Task.CompletedTask;
    }
}

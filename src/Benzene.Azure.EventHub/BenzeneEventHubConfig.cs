using Azure.Messaging.EventHubs.Consumer;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Configures the processing behavior used by <see cref="BenzeneEventHubWorker"/>. Which hub,
/// consumer group, and checkpoint store to use are decided by the
/// <c>EventProcessorClient</c> the caller builds (see <see cref="IEventProcessorClientFactory"/>) -
/// this config only covers what Benzene itself decides.
/// </summary>
public class BenzeneEventHubConfig
{
    /// <summary>
    /// Gets or sets where a partition starts reading when it has <em>no stored checkpoint</em> yet
    /// (a fresh consumer group, or a partition never checkpointed before). Defaults to <c>null</c>,
    /// which leaves the <c>EventProcessorClient</c>'s own default of reading from the <em>end</em> of
    /// the partition (<see cref="EventPosition.Latest"/>) - so only events enqueued after the
    /// processor claims the partition are seen. Set to <see cref="EventPosition.Earliest"/> to
    /// instead process the full retained backlog on first run. Once a partition has a checkpoint,
    /// that checkpoint always wins and this value is ignored - it only governs the very first read.
    /// The Kafka equivalent is <c>ConsumerConfig.AutoOffsetReset</c>.
    /// </summary>
    public EventPosition? DefaultStartingPosition { get; set; }

    /// <summary>
    /// Gets or sets how many successfully handled events a partition accumulates before its
    /// checkpoint is updated. Defaults to 1 - checkpoint after every event, the safest setting.
    /// Each checkpoint is a blob write, so raise this for throughput at the cost of a larger
    /// replay window: on restart/rebalance, a partition resumes from its last checkpoint, so up to
    /// <c>CheckpointInterval - 1</c> already-handled events can be redelivered.
    /// </summary>
    public int CheckpointInterval { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether an unhandled exception from an event's handler is caught (logged, the
    /// partition keeps processing, and - since Event Hubs is a stream with no per-event
    /// retry/dead-letter - the failed event is effectively skipped once a later event checkpoints
    /// past it). Defaults to <c>true</c>, matching both <c>BenzeneKafkaConfig.CatchHandlerExceptions</c>
    /// and the Azure Functions Event Hub trigger's checkpoint-advances-regardless behavior. Set to
    /// <c>false</c> to instead stop the whole worker on the first unhandled handler exception,
    /// without checkpointing the failed event - so a restart resumes from the last checkpoint and
    /// redelivers it (at-least-once).
    /// </summary>
    public bool CatchHandlerExceptions { get; set; } = true;
}

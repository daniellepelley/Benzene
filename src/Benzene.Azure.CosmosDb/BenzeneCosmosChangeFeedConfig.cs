namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Configures the processing behavior used by <see cref="BenzeneCosmosChangeFeedWorker{TDocument}"/>.
/// Which container to monitor, which lease container to use, and how to authenticate are decided by
/// the <c>ChangeFeedProcessor</c> the caller builds (see
/// <see cref="ICosmosChangeFeedProcessorFactory{TDocument}"/>) - this config only covers what
/// Benzene itself decides.
/// </summary>
public class BenzeneCosmosChangeFeedConfig
{
    /// <summary>
    /// Gets or sets whether the batch is checkpointed automatically after the pipeline completes
    /// successfully without the handler having called the context's checkpointer itself. Defaults
    /// to <c>true</c>, matching the Azure Functions <c>CosmosDBTrigger</c>'s
    /// checkpoint-on-successful-return behavior - a handler that never thinks about checkpointing
    /// gets sensible at-least-once semantics for free. Set to <c>false</c> for fully manual
    /// control: the batch is then only checkpointed when the handler calls
    /// <c>context.Checkpointer.CheckpointAsync(...)</c>, and a batch the handler never checkpoints
    /// is redelivered after a restart or lease rebalance (the processor still moves forward
    /// in-memory within the current lease ownership).
    /// </summary>
    public bool AutoCheckpointOnSuccess { get; set; } = true;

    /// <summary>
    /// Gets or sets whether an unhandled exception from the batch's pipeline is caught (logged,
    /// the batch is checkpointed anyway, and processing continues - i.e. the poison batch is
    /// <em>permanently skipped</em>). Defaults to <c>false</c>: the exception propagates to the
    /// Change Feed Processor, which does not advance the lease and redelivers the same batch -
    /// the platform-native at-least-once behavior. Note this default is the opposite of
    /// <c>BenzeneEventHubConfig.CatchHandlerExceptions</c>: Event Hubs has no per-batch redelivery,
    /// so skipping is its only way to keep going, whereas the change feed retries a failed batch
    /// natively - a reliably failing batch therefore retries forever under the default, so either
    /// handle poison documents inside the pipeline or opt in to skipping here.
    /// </summary>
    public bool CatchHandlerExceptions { get; set; }
}

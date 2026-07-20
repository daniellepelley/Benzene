namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Configures the processing behavior of <see cref="BenzeneCosmosAllVersionsChangeFeedWorker{TDocument}"/>.
/// Unlike <see cref="BenzeneCosmosChangeFeedConfig"/> there is no auto-checkpoint knob: the SDK's
/// all-versions-and-deletes processor is automatic-checkpoint only (it checkpoints after the handler
/// returns successfully), so the only decision Benzene owns here is what happens on a handler failure.
/// </summary>
public class BenzeneCosmosAllVersionsChangeFeedConfig
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from the batch's pipeline is caught (logged and
    /// swallowed, so the automatic checkpoint advances and the poison batch is <em>permanently
    /// skipped</em>). Defaults to <c>false</c>: the exception propagates to the Change Feed Processor,
    /// which does not checkpoint and redelivers the same batch - the platform-native at-least-once
    /// behavior (a reliably failing batch therefore retries forever under the default, so either handle
    /// poison changes inside the pipeline or opt in to skipping here). Mirrors
    /// <see cref="BenzeneCosmosChangeFeedConfig.CatchHandlerExceptions"/>.
    /// </summary>
    public bool CatchHandlerExceptions { get; set; }
}

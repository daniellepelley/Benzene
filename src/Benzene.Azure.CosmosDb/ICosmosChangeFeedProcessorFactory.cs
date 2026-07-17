using Microsoft.Azure.Cosmos;

namespace Benzene.Azure.CosmosDb;

/// <summary>
/// Creates the underlying <see cref="ChangeFeedProcessor"/> used by
/// <see cref="BenzeneCosmosChangeFeedWorker{TDocument}"/> to consume a container's change feed.
/// Lets the caller decide the monitored container, lease container, processor/instance names,
/// poll interval, batch size, start time, and authentication (connection string, Managed Identity
/// via a <c>TokenCredential</c>, emulator, ...) without the worker prescribing any of it. Unlike
/// Event Hubs' <c>EventProcessorClient</c>, the Cosmos SDK requires the change handler at
/// builder time, so the worker passes its delegates in rather than attaching them afterwards.
/// </summary>
/// <typeparam name="TDocument">The document type the change feed batches are deserialized into.</typeparam>
public interface ICosmosChangeFeedProcessorFactory<TDocument>
{
    /// <summary>
    /// Creates a <see cref="ChangeFeedProcessor"/> that delivers change batches to
    /// <paramref name="onChanges"/> (with manual checkpoint control) and errors to
    /// <paramref name="onError"/>.
    /// </summary>
    /// <param name="onChanges">The worker's batch handler, including its manual checkpoint hook.</param>
    /// <param name="onError">The worker's error handler for lease/processing failures.</param>
    /// <returns>The created (not yet started) processor.</returns>
    ChangeFeedProcessor Create(
        Container.ChangeFeedHandlerWithManualCheckpoint<TDocument> onChanges,
        Container.ChangeFeedMonitorErrorDelegate onError);
}

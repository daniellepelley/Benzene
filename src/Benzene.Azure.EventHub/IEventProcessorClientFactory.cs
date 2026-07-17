using Azure.Messaging.EventHubs;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Creates the underlying <see cref="EventProcessorClient"/> used by <see cref="BenzeneEventHubWorker"/>
/// to consume an Event Hub. Lets the caller decide the hub, consumer group, blob checkpoint
/// container, and authentication (connection string, Managed Identity via a <c>TokenCredential</c>,
/// emulator, ...) without the worker prescribing any of it.
/// </summary>
public interface IEventProcessorClientFactory
{
    /// <summary>
    /// Creates an <see cref="EventProcessorClient"/>.
    /// </summary>
    /// <returns>The created client.</returns>
    EventProcessorClient Create();
}

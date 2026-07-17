using Azure.Messaging.ServiceBus;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Creates the underlying <see cref="ServiceBusClient"/> used by <see cref="BenzeneServiceBusWorker"/>
/// to consume an entity. Lets the caller decide how the client is authenticated (connection string,
/// Managed Identity via a <c>TokenCredential</c>, emulator, ...) without the worker prescribing it.
/// </summary>
public interface IServiceBusClientFactory
{
    /// <summary>
    /// Creates a <see cref="ServiceBusClient"/>. The worker disposes the returned client when it stops.
    /// </summary>
    /// <returns>The created client.</returns>
    ServiceBusClient Create();
}

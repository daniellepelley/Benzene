using Azure.Messaging.ServiceBus;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Creates <see cref="ServiceBusClient"/> instances by returning the injected client instance.
/// </summary>
public class ServiceBusClientFactory : IServiceBusClientFactory
{
    private readonly ServiceBusClient _serviceBusClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusClientFactory"/> class.
    /// </summary>
    /// <param name="serviceBusClient">The Service Bus client to return from <see cref="Create"/>.</param>
    public ServiceBusClientFactory(ServiceBusClient serviceBusClient)
    {
        _serviceBusClient = serviceBusClient;
    }

    /// <summary>
    /// Returns the injected <see cref="ServiceBusClient"/>.
    /// </summary>
    /// <returns>The Service Bus client.</returns>
    public ServiceBusClient Create()
    {
        return _serviceBusClient;
    }
}

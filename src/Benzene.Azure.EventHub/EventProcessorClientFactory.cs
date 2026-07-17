using Azure.Messaging.EventHubs;

namespace Benzene.Azure.EventHub;

/// <summary>
/// Creates <see cref="EventProcessorClient"/> instances by returning the injected client instance.
/// </summary>
public class EventProcessorClientFactory : IEventProcessorClientFactory
{
    private readonly EventProcessorClient _eventProcessorClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventProcessorClientFactory"/> class.
    /// </summary>
    /// <param name="eventProcessorClient">The processor client to return from <see cref="Create"/>.</param>
    public EventProcessorClientFactory(EventProcessorClient eventProcessorClient)
    {
        _eventProcessorClient = eventProcessorClient;
    }

    /// <summary>
    /// Returns the injected <see cref="EventProcessorClient"/>.
    /// </summary>
    /// <returns>The processor client.</returns>
    public EventProcessorClient Create()
    {
        return _eventProcessorClient;
    }
}

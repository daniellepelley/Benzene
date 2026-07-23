using Azure.Messaging.EventHubs;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Results;

namespace Benzene.Azure.EventHub.TestHelpers;

/// <summary>
/// A test host that drives the Event Hub consumer pipeline a <c>StartUp</c> configured, without a
/// running hub. Built by <see cref="BenzeneTestHostExtensions.BuildEventHubWorkerHost{TStartUp}"/>;
/// push an event through it with <see cref="HandleAsync(EventData)"/> (build one from a
/// <c>MessageBuilder</c> via <c>AsEventHubBenzeneMessage()</c>).
/// </summary>
public sealed class EventHubWorkerBenzeneTestHost : IDisposable
{
    private readonly EventHubConsumerApplication _application;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>Initializes a new instance of the <see cref="EventHubWorkerBenzeneTestHost"/> class.</summary>
    /// <param name="application">The built Event Hub consumer application.</param>
    /// <param name="serviceResolverFactory">The resolver factory the application runs each event against.</param>
    public EventHubWorkerBenzeneTestHost(EventHubConsumerApplication application, IServiceResolverFactory serviceResolverFactory)
    {
        _application = application;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Runs an event through the pipeline exactly as <c>BenzeneEventHubWorker</c> would, returning the
    /// handler's recorded result.
    /// </summary>
    /// <param name="eventData">The event to handle.</param>
    /// <returns>The handler's result, or <c>null</c> if nothing set one.</returns>
    public Task<IBenzeneResult?> HandleAsync(EventData eventData)
    {
        return _application.HandleAsync(eventData, _serviceResolverFactory);
    }

    /// <summary>Disposes the resolver factory (and the service provider it owns).</summary>
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}

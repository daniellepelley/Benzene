using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;

namespace Benzene.Azure.ServiceBus.TestHelpers;

/// <summary>
/// A test host that drives the Service Bus consumer pipeline a <c>StartUp</c> configured, without a
/// running broker. Built by <see cref="BenzeneTestHostExtensions.BuildServiceBusWorkerHost{TStartUp}"/>;
/// push a message through it with <see cref="HandleAsync(ServiceBusReceivedMessage)"/> (build one from
/// a <c>MessageBuilder</c> via <c>AsAzureServiceBusMessage()</c>).
/// </summary>
public sealed class ServiceBusWorkerBenzeneTestHost : IDisposable
{
    private readonly ServiceBusConsumerApplication _application;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>Initializes a new instance of the <see cref="ServiceBusWorkerBenzeneTestHost"/> class.</summary>
    /// <param name="application">The built Service Bus consumer application.</param>
    /// <param name="serviceResolverFactory">The resolver factory the application runs each message against.</param>
    public ServiceBusWorkerBenzeneTestHost(ServiceBusConsumerApplication application, IServiceResolverFactory serviceResolverFactory)
    {
        _application = application;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Runs a message through the pipeline exactly as <c>BenzeneServiceBusWorker</c> would, returning
    /// the settlement decision (the handler's result plus any explicit settlement override).
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <returns>The settlement decision.</returns>
    public Task<ServiceBusSettlementDecision> HandleAsync(ServiceBusReceivedMessage message)
    {
        return _application.HandleAsync(message, _serviceResolverFactory);
    }

    /// <summary>Disposes the resolver factory (and the service provider it owns).</summary>
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}

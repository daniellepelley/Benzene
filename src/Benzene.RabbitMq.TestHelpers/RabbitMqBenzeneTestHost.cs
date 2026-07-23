using Benzene.Abstractions.DI;
using Benzene.Abstractions.Results;
using Benzene.RabbitMq.RabbitMqMessage;
using RabbitMQ.Client.Events;

namespace Benzene.RabbitMq.TestHelpers;

/// <summary>
/// A test host that drives the RabbitMQ message pipeline a <c>StartUp</c> configured, without a
/// running broker. Built by <see cref="BenzeneTestHostExtensions.BuildRabbitMqWorkerHost{TStartUp}"/>; push
/// a delivery through it with <see cref="HandleAsync(BasicDeliverEventArgs)"/> (build one from a
/// <c>MessageBuilder</c> via <c>AsRabbitMqBenzeneMessage()</c>).
/// </summary>
public sealed class RabbitMqBenzeneTestHost : IDisposable
{
    private readonly RabbitMqApplication _application;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>Initializes a new instance of the <see cref="RabbitMqBenzeneTestHost"/> class.</summary>
    /// <param name="application">The built RabbitMQ message application.</param>
    /// <param name="serviceResolverFactory">The resolver factory the application runs each delivery against.</param>
    public RabbitMqBenzeneTestHost(RabbitMqApplication application, IServiceResolverFactory serviceResolverFactory)
    {
        _application = application;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Runs a delivery through the pipeline exactly as <c>RabbitMqWorker</c> would, returning the
    /// handler's recorded result (used by the worker to decide ack/nack).
    /// </summary>
    /// <param name="delivery">The delivery to handle.</param>
    /// <returns>The handler's result, or <c>null</c> if nothing set one.</returns>
    public Task<IBenzeneResult?> HandleAsync(BasicDeliverEventArgs delivery)
    {
        return _application.HandleAsync(delivery, _serviceResolverFactory);
    }

    /// <summary>Disposes the resolver factory (and the service provider it owns).</summary>
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}

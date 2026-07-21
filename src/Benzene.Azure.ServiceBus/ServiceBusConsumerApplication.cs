using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Processes a single received Service Bus message by mapping it to a
/// <see cref="ServiceBusConsumerContext"/> and running it through the middleware pipeline in its own
/// service scope, tagging the transport as <c>"service-bus"</c> for the duration. Returns a
/// <see cref="ServiceBusSettlementDecision"/> carrying the handler's recorded
/// <see cref="Benzene.Abstractions.Results.IBenzeneResult"/> and any explicit settlement the
/// handler requested via <see cref="ServiceBusSettlementHolder"/>, which
/// <see cref="BenzeneServiceBusWorker"/> reads for <see cref="ServiceBusConsumerAckMode.Explicit"/>.
/// </summary>
/// <remarks>
/// Owns its own DI scope (rather than extending <c>MiddlewareApplication</c>) so it can resolve the
/// scoped <see cref="ServiceBusSettlementHolder"/> the handler mutated — the worker runs outside that
/// scope and can't read it directly. The scope is disposed once the decision has been extracted.
/// </remarks>
public class ServiceBusConsumerApplication
{
    private readonly IMiddlewarePipeline<ServiceBusConsumerContext> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusConsumerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Service Bus middleware pipeline to run each message through.</param>
    public ServiceBusConsumerApplication(IMiddlewarePipeline<ServiceBusConsumerContext> pipeline)
    {
        _pipeline = new TransportMiddlewarePipeline<ServiceBusConsumerContext>(TransportNames.ServiceBus, pipeline);
    }

    /// <summary>
    /// Runs the message through the pipeline in a fresh scope and returns the settlement decision.
    /// </summary>
    /// <param name="message">The received Service Bus message.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create the per-message scope.</param>
    /// <param name="cancellationToken">The transport's cancellation token for this message.</param>
    /// <returns>The handler's result plus any explicit settlement override.</returns>
    public async Task<ServiceBusSettlementDecision> HandleAsync(ServiceBusReceivedMessage message,
        IServiceResolverFactory serviceResolverFactory, CancellationToken cancellationToken = default)
    {
        var context = ServiceBusConsumerContext.CreateInstance(message);
        using var serviceResolver = serviceResolverFactory.CreateScope();
        serviceResolver.SeedCancellationToken(cancellationToken);

        await _pipeline.HandleAsync(context, serviceResolver);

        var settlement = serviceResolver.TryGetService<ServiceBusSettlementHolder>();
        return new ServiceBusSettlementDecision(context.MessageResult, settlement);
    }
}

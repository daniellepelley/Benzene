using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Azure.ServiceBus;
using Benzene.HealthChecks.Core;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Registration helpers that auto-wire the consumer-side <see cref="ServiceBusHealthCheck"/> from the
/// worker's config + client factory. Auto-wiring lives on the <b>consumer</b> (not the sender): the
/// peek-based check needs the <c>Listen</c> claim the consumer holds, and it knows the exact entity
/// (queue, or topic + subscription) it consumes — the sender has neither (see
/// <c>work/client-health-checks-remaining-designs.md</c> §7).
/// </summary>
public static class ServiceBusHealthCheckExtensions
{
    /// <summary>
    /// Auto-registers a <see cref="ServiceBusHealthCheck"/> for the consumed entity on the <b>dependency</b>
    /// category (deep <c>healthcheck</c> layer only — never a Kubernetes probe; see
    /// <see cref="IDependencyHealthCheck"/>), deduped by the entity. Called by
    /// <c>UseServiceBus(..., healthCheck: true)</c>. One <see cref="global::Azure.Messaging.ServiceBus.ServiceBusClient"/>
    /// is created from the factory and reused across probes (the check opens a short-lived receiver per probe).
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="config">The consumer config identifying the queue, or topic + subscription.</param>
    /// <param name="clientFactory">The factory used to create the (reused) Service Bus client.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddServiceBusDependencyHealthCheck(this IBenzeneServiceContainer services,
        BenzeneServiceBusConfig config, IServiceBusClientFactory clientFactory)
    {
        // ServiceBusClient construction is lazy (no connection until a receiver is used), so creating it
        // here and reusing it is cheap and avoids a per-probe client.
        var client = clientFactory.Create();
        var isQueue = !string.IsNullOrEmpty(config.QueueName);

        var check = isQueue
            ? new ServiceBusHealthCheck(client, config.QueueName!)
            : new ServiceBusHealthCheck(client, config.TopicName!, config.SubscriptionName!);

        var dedupKey = isQueue
            ? $"ServiceBus:{config.QueueName}"
            : $"ServiceBus:{config.TopicName}/{config.SubscriptionName}";

        return services.AddDependencyHealthCheck(_ => check, dedupKey);
    }
}

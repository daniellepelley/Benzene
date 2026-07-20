using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Azure.ServiceBus;

/// <summary>Registration helpers for <see cref="ServiceBusHealthCheck"/>.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <see cref="ServiceBusHealthCheck"/> for a queue, resolving <c>ServiceBusClient</c>
    /// from DI (the consumer must register it).
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="queueName">The queue to check.</param>
    public static IHealthCheckBuilder AddServiceBusQueueHealthCheck(this IHealthCheckBuilder builder, string queueName)
    {
        return builder.AddHealthCheckFactory(new ServiceBusHealthCheckFactory(queueName));
    }

    /// <summary>
    /// Registers a <see cref="ServiceBusHealthCheck"/> for a topic subscription, resolving
    /// <c>ServiceBusClient</c> from DI (the consumer must register it).
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="topicName">The topic the subscription belongs to.</param>
    /// <param name="subscriptionName">The subscription to check.</param>
    public static IHealthCheckBuilder AddServiceBusSubscriptionHealthCheck(this IHealthCheckBuilder builder, string topicName, string subscriptionName)
    {
        return builder.AddHealthCheckFactory(new ServiceBusHealthCheckFactory(topicName, subscriptionName));
    }
}

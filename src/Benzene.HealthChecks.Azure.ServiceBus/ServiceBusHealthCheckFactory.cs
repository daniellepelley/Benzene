using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Azure.ServiceBus;

/// <summary>
/// Builds a <see cref="ServiceBusHealthCheck"/> for a fixed queue or topic subscription, resolving the
/// <see cref="ServiceBusClient"/> from DI (the consumer registers it - Benzene never wraps the client's
/// authentication choice).
/// </summary>
public class ServiceBusHealthCheckFactory : IHealthCheckFactory
{
    private readonly string? _queueName;
    private readonly string? _topicName;
    private readonly string? _subscriptionName;

    /// <summary>Builds a check for a queue.</summary>
    /// <param name="queueName">The queue the resulting health check will peek.</param>
    public ServiceBusHealthCheckFactory(string queueName)
    {
        _queueName = queueName;
    }

    /// <summary>Builds a check for a topic subscription.</summary>
    /// <param name="topicName">The topic the subscription belongs to.</param>
    /// <param name="subscriptionName">The subscription the resulting health check will peek.</param>
    public ServiceBusHealthCheckFactory(string topicName, string subscriptionName)
    {
        _topicName = topicName;
        _subscriptionName = subscriptionName;
    }

    /// <inheritdoc />
    public IHealthCheck Create(IServiceResolver resolver)
    {
        var client = resolver.GetService<ServiceBusClient>();
        return _queueName != null
            ? new ServiceBusHealthCheck(client, _queueName)
            : new ServiceBusHealthCheck(client, _topicName!, _subscriptionName!);
    }
}

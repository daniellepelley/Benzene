using System;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Configures the entity and processing behavior used by <see cref="BenzeneServiceBusWorker"/>.
/// Set either <see cref="QueueName"/> or both <see cref="TopicName"/> and
/// <see cref="SubscriptionName"/> - exactly one entity kind, validated at worker startup.
/// </summary>
public class BenzeneServiceBusConfig
{
    /// <summary>
    /// Gets or sets the queue to consume from. Mutually exclusive with
    /// <see cref="TopicName"/>/<see cref="SubscriptionName"/>.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the topic to consume from. Requires <see cref="SubscriptionName"/> and is
    /// mutually exclusive with <see cref="QueueName"/>.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets the subscription on <see cref="TopicName"/> to consume from.
    /// </summary>
    public string? SubscriptionName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages handled concurrently
    /// (<c>ServiceBusProcessorOptions.MaxConcurrentCalls</c>). Defaults to 5, matching
    /// <c>BenzeneKafkaConfig.ConcurrentRequests</c>.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 5;

    /// <summary>
    /// Gets or sets how many additional messages the processor requests ahead of processing
    /// (<c>ServiceBusProcessorOptions.PrefetchCount</c>). Defaults to 0 (no prefetch), the
    /// Service Bus SDK default - prefetched messages count against their lock duration from the
    /// moment they're fetched, so prefetching is only a win when handlers are fast.
    /// </summary>
    public int PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets whether a message's settlement is left to the processor's own auto-complete
    /// behavior, or explicitly controlled from the handler's outcome (including a non-exception
    /// failure result). Defaults to <see cref="ServiceBusConsumerAckMode.Explicit"/> - a handler that
    /// returns a failure result (not just one that throws) abandons the message for redelivery
    /// instead of silently completing it. Set <see cref="ServiceBusConsumerAckMode.AutoComplete"/> to
    /// hand settlement back to the processor (a non-exception failure result then completes). See the
    /// enum's own doc comments for the exact semantics of each mode.
    /// </summary>
    public ServiceBusConsumerAckMode AckMode { get; set; } = ServiceBusConsumerAckMode.Explicit;

    /// <summary>
    /// Gets or sets the application property the topic is read from. Defaults to
    /// <see cref="ServiceBusConsumerMessageTopicGetter.DefaultTopicProperty"/> (<c>"topic"</c>) — set
    /// a different key to consume messages a non-Benzene producer routes on another application
    /// property, without writing a custom topic getter. Keep it in sync with the producer's key.
    /// </summary>
    public string TopicPropertyKey { get; set; } = ServiceBusConsumerMessageTopicGetter.DefaultTopicProperty;

    /// <summary>
    /// Gets or sets the maximum total duration the processor renews a message's lock while a handler
    /// runs (<c>ServiceBusProcessorOptions.MaxAutoLockRenewalDuration</c>). <c>null</c> (the default)
    /// leaves the SDK default (5 minutes). Raise it for handlers that can legitimately run longer than
    /// the entity's lock duration, so the message isn't redelivered while still being processed.
    /// </summary>
    public TimeSpan? MaxAutoLockRenewalDuration { get; set; }
}

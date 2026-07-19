namespace Benzene.RabbitMq;

/// <summary>
/// The queue to consume and the processing behavior <see cref="RabbitMqWorker"/> uses. The worker
/// assumes the queue (and any dead-letter topology) already exists - declaring exchanges/queues is
/// out of scope here, exactly as the Kafka worker assumes its topics exist.
/// </summary>
public class RabbitMqConfig
{
    /// <summary>The name of the queue to consume.</summary>
    public required string QueueName { get; set; }

    /// <summary>
    /// The message-property header the topic is read from (falling back to the AMQP routing key when
    /// absent). Defaults to <see cref="RabbitMqConstants.DefaultTopicHeader"/> (<c>"topic"</c>) — set a
    /// different key to consume messages a non-Benzene producer routes on another header, without
    /// writing a custom topic getter. Keep it in sync with the producer's header key.
    /// </summary>
    public string TopicHeaderKey { get; set; } = RabbitMqConstants.DefaultTopicHeader;

    /// <summary>
    /// The consumer prefetch (QoS) count - the maximum number of unacknowledged deliveries the broker
    /// will hand this consumer at once. Bounds in-flight work and provides backpressure; set at or
    /// above <see cref="ConcurrentRequests"/> so every lane can stay fed. Default 5.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 5;

    /// <summary>
    /// The maximum number of deliveries handled concurrently, across the worker's dispatcher lanes.
    /// RabbitMQ makes no ordering promise across a queue once more than one delivery is in flight, so
    /// deliveries round-robin across lanes with no per-key affinity. Default 5.
    /// </summary>
    public int ConcurrentRequests { get; set; } = 5;

    /// <summary>The settlement mode. Default <see cref="RabbitMqAckMode.Explicit"/>.</summary>
    public RabbitMqAckMode AckMode { get; set; } = RabbitMqAckMode.Explicit;

    /// <summary>
    /// Under <see cref="RabbitMqAckMode.Explicit"/>, whether a failed delivery is requeued for
    /// another attempt (<c>true</c>, the default) or nacked without requeue (<c>false</c>) so it is
    /// routed to the queue's dead-letter exchange, if one is configured, or dropped otherwise.
    /// </summary>
    /// <remarks>
    /// Requeue is <em>bounded</em> to avoid a poison-message hot loop: a delivery that fails on its
    /// first attempt is requeued, but a delivery that is <em>already redelivered</em> and fails again
    /// is nacked without requeue (to the DLX / dropped). RabbitMQ's redelivered flag is a single
    /// boolean, not a count, so this is a one-retry bound - for a higher, precise redelivery limit,
    /// set <see cref="RequeueOnFailure"/> to <c>false</c> and configure a dead-letter exchange with a
    /// queue policy on the broker (the production setting). Quorum-queue delivery-count features are
    /// out of scope for this worker.
    /// </remarks>
    public bool RequeueOnFailure { get; set; } = true;

    /// <summary>
    /// The maximum time <c>StopAsync</c> waits for in-flight handlers to finish before abandoning
    /// them and closing the channel/connection. Default 30 seconds.
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

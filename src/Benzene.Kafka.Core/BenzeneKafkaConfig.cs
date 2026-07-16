using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public class BenzeneKafkaConfig
{
    public required ConsumerConfig ConsumerConfig { get; set; }
    public required string[] Topics { get; set; }

    /// <summary>The maximum number of messages handled concurrently.</summary>
    public int ConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// When <c>true</c> (the default), messages from the same partition are always dispatched to
    /// the same lane, so a partition's messages are handled in order - standard Kafka consumer
    /// behavior, since order is only ever promised within a partition. Different partitions still
    /// run concurrently, up to <see cref="ConcurrentRequests"/> at once. Set to <c>false</c> for
    /// unordered round-robin dispatch instead, when throughput matters more than per-partition order.
    /// </summary>
    public bool PreserveOrderPerPartition { get; set; } = true;

    /// <summary>
    /// The maximum time <c>StopAsync</c> waits for in-flight message handlers to finish before
    /// abandoning them and closing the consumer.
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long to wait after a <see cref="Confluent.Kafka.ConsumeException"/> before retrying
    /// <c>Consume</c> again. Without this, a persistently failing broker/connection (as opposed to a
    /// single bad message) would otherwise spin the consume loop in a tight, uncancellable-until-next-
    /// iteration retry loop - logging and burning CPU on every failed attempt.
    /// </summary>
    public TimeSpan ConsumeExceptionRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, that
    /// lane keeps consuming) or left to stop the worker entirely. Defaults to <c>true</c> (catch) -
    /// a single bad message shouldn't take down the whole consumer. Set to <c>false</c> to instead
    /// stop the worker on the first unhandled handler exception (the same effect as calling
    /// <c>StopAsync</c>) - useful when a handler exception should be treated as fatal rather than
    /// silently logged and skipped.
    /// </summary>
    public bool CatchHandlerExceptions { get; set; } = true;
}
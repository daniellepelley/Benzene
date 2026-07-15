using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public class BenzeneKafkaConfig
{
    public ConsumerConfig ConsumerConfig { get; set; }
    public string[] Topics { get; set; }

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
}
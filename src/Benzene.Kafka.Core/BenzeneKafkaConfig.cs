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

    /// <summary>
    /// Gets or sets whether an offset is only stored (and so only eligible to be committed) after
    /// its message's handler has completed successfully, instead of Confluent.Kafka's default of
    /// auto-storing the offset as soon as <c>Consume</c> returns the message - before it's actually
    /// been handled. Defaults to <c>false</c> (auto-store on consume, the Confluent.Kafka default).
    /// Set to <c>true</c> for at-least-once processing: a message whose handler fails (or whose
    /// worker crashes mid-handling) is redelivered on restart/rebalance instead of being silently
    /// skipped.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="CatchHandlerExceptions"/> = <c>false</c> and
    /// <see cref="PreserveOrderPerPartition"/> = <c>true</c> - enforced at worker startup. Both are
    /// load-bearing: Confluent.Kafka's <c>StoreOffset</c> is a last-write-wins watermark with no gap
    /// tracking, so storing a later offset silently commits past any earlier message that hasn't
    /// actually been stored yet. Catching a handler exception (rather than stopping the worker)
    /// would let a later, successful message on the same partition store its offset while the
    /// failed one's was never stored - silently skipping the failed message on the next commit.
    /// Requiring <c>PreserveOrderPerPartition</c> guarantees a partition's messages are only ever
    /// handled - and so only ever stored - one at a time, in order, so the watermark never advances
    /// past a message that hasn't actually succeeded yet.
    /// </remarks>
    public bool CommitOnlyOnSuccess { get; set; } = false;

    /// <summary>
    /// Gets or sets whether, on a consumer-group rebalance, the worker drains in-flight handlers for
    /// the revoked partitions and commits their stored offsets before releasing them - so no record is
    /// committed as done while still being handled, and none is silently reprocessed by the partition's
    /// next owner. <c>null</c> (the default) resolves to <see cref="CommitOnlyOnSuccess"/>: draining is
    /// strictly safer under at-least-once and pointless under auto-store, so it defaults on exactly when
    /// <see cref="CommitOnlyOnSuccess"/> is on. Set explicitly to override. Draining is bounded by
    /// <see cref="DrainTimeout"/>. Wired via the consumer's <c>SetPartitionsRevokedHandler</c> - only
    /// honored when the consumer is built through the default <see cref="KafkaConsumerFactory{TKey,TValue}"/>
    /// or a custom factory that applies the worker's builder-configuration callback.
    /// </summary>
    public bool? DrainOnRevoke { get; set; }

    /// <summary>Resolves <see cref="DrainOnRevoke"/> to its effective value (defaults to <see cref="CommitOnlyOnSuccess"/>).</summary>
    public bool ShouldDrainOnRevoke => DrainOnRevoke ?? CommitOnlyOnSuccess;
}
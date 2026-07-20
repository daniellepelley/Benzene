using Confluent.Kafka;

namespace Benzene.Kafka.Core;

/// <summary>
/// Opt-in retry-then-dead-letter policy for <see cref="BenzeneKafkaWorker{TKey,TValue}"/>. When a
/// record's handler keeps failing, the worker retries it up to <see cref="MaxAttempts"/> times and,
/// if it still fails, re-produces the <em>original</em> record (key, value, headers) to
/// <see cref="DeadLetterTopic"/> with diagnostic <c>x-dlt-*</c> headers, then advances past it - so a
/// poison record neither wedges the partition nor is silently lost. Off unless
/// <see cref="DeadLetterTopic"/> is set. Benzene never wraps the producer's auth: the caller builds
/// and owns <see cref="Producer"/>.
/// </summary>
/// <typeparam name="TKey">The record key type (matches the worker's consumer).</typeparam>
/// <typeparam name="TValue">The record value type (matches the worker's consumer).</typeparam>
public class KafkaDeadLetterOptions<TKey, TValue>
{
    /// <summary>The header carrying the failing exception's type name (never the message text).</summary>
    public const string ReasonHeader = "x-dlt-reason";

    /// <summary>The header carrying the record's original topic.</summary>
    public const string OriginalTopicHeader = "x-dlt-original-topic";

    /// <summary>The header carrying the record's original partition.</summary>
    public const string OriginalPartitionHeader = "x-dlt-original-partition";

    /// <summary>The header carrying the record's original offset.</summary>
    public const string OriginalOffsetHeader = "x-dlt-original-offset";

    /// <summary>
    /// Gets or sets the topic failed records are re-produced to (e.g. <c>"orders.DLT"</c>).
    /// <c>null</c> (the default) disables dead-lettering entirely.
    /// </summary>
    public string? DeadLetterTopic { get; set; }

    /// <summary>
    /// Gets or sets the number of in-process handler attempts before dead-lettering (default 1 - one
    /// attempt, no retry). Each attempt re-runs the handler with the record's existing key/headers.
    /// </summary>
    public int MaxAttempts { get; set; } = 1;

    /// <summary>
    /// Gets or sets the caller-built producer used to publish to <see cref="DeadLetterTopic"/>. Benzene
    /// does not build or wrap it - the caller owns its config and auth (matching the consumer factory
    /// seam). Required when <see cref="DeadLetterTopic"/> is set.
    /// </summary>
    public IProducer<TKey, TValue>? Producer { get; set; }

    /// <summary>Gets whether dead-lettering is enabled (a topic and a producer are both configured).</summary>
    public bool IsEnabled => !string.IsNullOrEmpty(DeadLetterTopic) && Producer != null;
}

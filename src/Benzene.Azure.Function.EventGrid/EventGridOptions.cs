namespace Benzene.Azure.Function.EventGrid;

/// <summary>
/// Configures how <see cref="EventGridApplication"/> handles a message handler's exceptions and
/// failure results. Mirrors <c>Benzene.Azure.Function.Kafka</c>'s <c>KafkaOptions</c>.
/// </summary>
public class EventGridOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, and the
    /// invocation reports success - so Event Grid sees a delivered event and does not retry) instead
    /// of left to cascade and fail the trigger invocation (Event Grid's own retry/dead-letter policy
    /// applies). Defaults to <c>false</c> - an exception usually signals a transient failure worth
    /// retrying and eventually dead-lettering.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result is escalated
    /// into a thrown <see cref="EventGridMessageProcessingException"/>, so Event Grid retries the
    /// event the same way it would for an unhandled exception (its retry runs with backoff up to 24h,
    /// then dead-letters). Defaults to <c>true</c> (safe-by-default: a returned failure is escalated and redelivered; set <c>false</c> for at-most-once, and keep the handler idempotent). Historically the reasoning was that a failure result usually reflects a
    /// permanent/business failure retrying won't fix. Turn it on for at-least-once semantics; the
    /// handler must then be idempotent.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of events from a single batched delivery processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded. A value &lt;= 0 is treated the same as
    /// <c>null</c>. Applies to batched delivery (trigger cardinality "many").
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

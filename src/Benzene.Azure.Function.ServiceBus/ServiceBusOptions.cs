namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Configures how <see cref="ServiceBusApplication"/> handles a message handler's exceptions and
/// failure results.
/// </summary>
public class ServiceBusOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, that
    /// message's failure doesn't affect the rest of the batch) instead of left to cascade and fail
    /// the whole trigger invocation. Defaults to <c>false</c> - there is no explicit per-message
    /// complete/abandon control wired up in this package (see the package's <c>CLAUDE.md</c>), so
    /// an uncaught exception failing the whole invocation is the only way the Functions host's own
    /// retry policy notices anything went wrong.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result is escalated
    /// into a thrown exception, so a failure is treated the same as an unhandled exception for retry
    /// purposes. Defaults to <c>true</c> (safe-by-default: a returned failure is escalated and redelivered; set <c>false</c> for at-most-once, and keep the handler idempotent). Historically the reasoning was that a failure result usually reflects a permanent/business-logic
    /// failure that retrying won't fix.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets whether each message's completion is left to the Functions host's own
    /// auto-complete behavior, or explicitly controlled based on the handler's outcome. Defaults to
    /// <see cref="ServiceBusAckMode.AutoComplete"/> (unchanged behavior). Set to
    /// <see cref="ServiceBusAckMode.Explicit"/> for true per-message complete/abandon control - see
    /// that enum's own doc comments for the trigger configuration it requires.
    /// </summary>
    public ServiceBusAckMode AckMode { get; set; } = ServiceBusAckMode.AutoComplete;

    /// <summary>
    /// Gets or sets the maximum number of messages from a single trigger batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded - every message in the batch starts at
    /// once, the original behavior. Set a positive value to cap concurrency, e.g. to stop a large
    /// batched trigger from opening more scoped database connections than the pool allows. A value
    /// &lt;= 0 is treated the same as <c>null</c> (unbounded). Applies to a batched trigger
    /// (<c>IsBatched = true</c>); a single-message trigger has nothing to bound.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Configures how <see cref="EventBridgeApplication"/> handles a message handler's exceptions and
/// failure results. Mirrors <c>Benzene.Aws.Lambda.Sns</c>'s <c>SnsOptions</c>.
/// </summary>
public class EventBridgeOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, and the
    /// Lambda invocation reports success - so the EventBridge rule target sees a delivered event and
    /// does not retry) instead of left to cascade out of the invocation (the target's own
    /// retry/on-failure-destination policy applies). Defaults to <c>false</c> - an exception usually
    /// signals a transient/unexpected failure worth retrying and eventually dead-lettering.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result (e.g. a
    /// validation error) is escalated into a thrown <see cref="EventBridgeMessageProcessingException"/>,
    /// so the EventBridge rule target retries the event the same way it would for an unhandled
    /// exception. Defaults to <c>true</c> (safe-by-default: a returned failure is escalated and redelivered; set <c>false</c> for at-most-once, and keep the handler idempotent). Historically the reasoning was that a failure result usually reflects a permanent/business
    /// failure that retrying won't fix. Turn it on for at-least-once semantics; the handler must then
    /// be idempotent.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;
}

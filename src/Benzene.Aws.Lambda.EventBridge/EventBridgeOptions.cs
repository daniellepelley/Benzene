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
    /// exception. Defaults to <c>true</c> (safe-by-default): a returned failure is escalated and
    /// redelivered (at-least-once), so the handler must be idempotent. Set <c>false</c> for
    /// at-most-once, where a failure result is accepted and the event is not retried.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;
}

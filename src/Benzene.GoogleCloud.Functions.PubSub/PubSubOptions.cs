namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Configures how <see cref="PubSubMiddlewareApplication"/> handles a message handler's exceptions
/// and failure results.
/// </summary>
public class PubSubOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged)
    /// instead of left to cascade and fail the whole invocation. Defaults to <c>false</c> - Cloud
    /// Functions Framework has no partial-failure mechanism (Pub/Sub delivers one message per
    /// invocation), so an uncaught exception - which Functions Framework turns into a non-2xx
    /// response - is the only way the subscription's own retry/dead-letter policy notices anything
    /// went wrong.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result is escalated
    /// into a thrown exception, so a failure is treated the same as an unhandled exception for
    /// retry purposes. Defaults to <c>true</c> (safe-by-default: a returned failure is escalated and redelivered; set <c>false</c> for at-most-once, and keep the handler idempotent). Historically the reasoning was that a failure result usually reflects a
    /// permanent/business-logic failure that retrying won't fix.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;
}

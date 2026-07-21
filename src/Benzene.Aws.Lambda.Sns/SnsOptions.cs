namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Configures how <see cref="SnsApplication"/> handles a message handler's exceptions and failure
/// results.
/// </summary>
public class SnsOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, and the
    /// Lambda invocation reports success to SNS - no retry) instead of left to cascade out of the
    /// invocation (SNS's own subscription retry policy applies). Defaults to <c>false</c> - an
    /// exception usually signals a transient/unexpected failure that's worth retrying (and eventually
    /// dead-lettering via the subscription's redrive policy); silently swallowing it risks losing the
    /// message forever.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result (e.g. a
    /// validation error) is escalated into a thrown exception, so SNS retries the notification the
    /// same way it would for an unhandled exception. Defaults to <c>true</c> - a returned failure is
    /// not silently settled, so the message is redelivered (and eventually dead-lettered via the
    /// subscription's redrive policy) rather than lost: at-least-once out of the box. Because a
    /// retried delivery re-runs the handler with the same message (SNS provides no dedup), the
    /// handler must be idempotent. Set to <c>false</c> for at-most-once, where a failure result is
    /// accepted and the notification is not retried.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of records from a single batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded - every record in the batch starts at
    /// once, the original behavior. Set a positive value to cap concurrency, e.g. to stop a large
    /// fan-out batch from opening more scoped database connections than the pool allows. A value
    /// &lt;= 0 is treated the same as <c>null</c> (unbounded).
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

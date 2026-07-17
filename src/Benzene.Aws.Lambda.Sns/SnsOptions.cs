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
    /// same way it would for an unhandled exception. Defaults to <c>false</c> - a failure result
    /// usually reflects a permanent/business-logic failure that retrying won't fix, so escalating it
    /// would waste invocations and delay visibility into the real problem.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = false;
}

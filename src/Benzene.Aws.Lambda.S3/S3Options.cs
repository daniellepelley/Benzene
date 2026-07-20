namespace Benzene.Aws.S3;

/// <summary>
/// Configures how <see cref="S3Application"/> handles a message handler's exceptions and failure
/// results. Mirrors <c>Benzene.Aws.Lambda.Sns</c>'s <c>SnsOptions</c>.
/// </summary>
public class S3Options
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, and the
    /// Lambda invocation reports success - so S3's async-invoke retry/on-failure destination does not
    /// engage) instead of left to cascade out of the invocation. Defaults to <c>false</c> - an
    /// exception usually signals a transient/unexpected failure worth retrying and eventually
    /// dead-lettering via the function's own retry/destination configuration.
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result (e.g. a
    /// validation error) is escalated into a thrown <see cref="S3MessageProcessingException"/>, so
    /// S3's async-invoke retry applies the same way it would for an unhandled exception. Defaults to
    /// <c>false</c> - a failure result usually reflects a permanent/business failure retrying won't
    /// fix. Turn it on for at-least-once semantics; the handler must then be idempotent.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of records from a single batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded - every record starts at once, the
    /// original behavior. A value &lt;= 0 is treated the same as <c>null</c>.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

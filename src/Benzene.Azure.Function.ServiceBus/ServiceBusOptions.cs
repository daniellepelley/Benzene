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
    /// purposes. Defaults to <c>false</c> - a failure result usually reflects a permanent/business-logic
    /// failure that retrying won't fix.
    /// </summary>
    public bool RaiseOnFailureStatus { get; set; } = false;
}

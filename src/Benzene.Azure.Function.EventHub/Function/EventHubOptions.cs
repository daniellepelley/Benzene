namespace Benzene.Azure.Function.EventHub.Function;

/// <summary>
/// Configures how <see cref="EventHubApplication"/> (via <see cref="EventHubBatchApplication"/>)
/// handles a message handler's exceptions and failure results while fanning a triggered batch of
/// events out across the middleware pipeline. Mirrors <c>Benzene.Azure.Function.QueueStorage</c>'s
/// <c>QueueStorageOptions</c> and <c>Benzene.Azure.Function.EventGrid</c>'s <c>EventGridOptions</c>.
/// </summary>
/// <remarks>
/// <para>
/// Both flags default to <c>false</c> and the fan-out defaults to unbounded, so the out-of-the-box
/// behavior is exactly what it was before this type existed: every event in the batch runs, an
/// unhandled exception cascades and fails the whole Functions invocation, and a non-exception
/// failure result checkpoints like a success.
/// </para>
/// <para>
/// <b>Ordering tradeoff.</b> Event Hub records within a partition are ordered, and the default
/// (both flags off) fan-out runs them concurrently but lets any exception fail the whole invocation
/// so the trigger re-delivers - and re-runs - the entire batch, siblings included. Turning
/// <see cref="CatchExceptions"/> on trades that all-or-nothing re-delivery for sibling isolation: a
/// poison event is logged and skipped so its already-succeeded (and not-yet-run) siblings are not
/// re-run, but the poison event is <b>not</b> retried and the batch still checkpoints past it. This
/// is the same ordering-for-isolation tradeoff <c>Benzene.Aws.Lambda.S3</c> and
/// <c>Benzene.Azure.Function.EventGrid</c> already accept; enable it only where per-event isolation
/// matters more than strict in-partition ordering/at-least-once re-delivery.
/// </para>
/// </remarks>
public class EventHubOptions
{
    /// <summary>
    /// Gets or sets whether an unhandled exception from a message handler is caught (logged, and the
    /// event skipped - so its siblings still complete and the batch checkpoints) instead of left to
    /// cascade and fail the whole Functions invocation. When an exception cascades, the Event Hubs
    /// trigger re-delivers the <b>entire</b> batch, so every already-succeeded sibling re-runs.
    /// Defaults to <c>false</c> - preserving the original all-or-nothing behavior. Turn it on to
    /// isolate a poison event from its siblings (see the ordering tradeoff on <see cref="EventHubOptions"/>).
    /// </summary>
    public bool CatchExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether a message handler returning a non-exception failure result is escalated
    /// into a thrown <see cref="EventHubMessageProcessingException"/>, so the invocation fails and the
    /// Event Hubs trigger re-delivers the batch the same way it would for an unhandled exception.
    /// Defaults to <c>true</c> - a returned failure is escalated and redelivered (at-least-once). Set <c>false</c> for
    /// at-most-once (a failure result is accepted, not retried); either way the handler must be idempotent.
    /// </summary>
    /// <remarks>
    /// This reads <see cref="EventHubContext.MessageResult"/>. In this package's default envelope
    /// routing path (<c>UseBenzeneMessage</c>) the handler runs on the inner <c>BenzeneMessageContext</c>
    /// and its response is suppressed, so nothing populates <see cref="EventHubContext.MessageResult"/>
    /// there today - meaning this flag only bites when a middleware/setter records a result directly on
    /// the <see cref="EventHubContext"/>. See the package CLAUDE.md "Failure handling" section.
    /// </remarks>
    public bool RaiseOnFailureStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of events from a single triggered batch processed concurrently.
    /// <c>null</c> (the default) leaves the fan-out unbounded - the original behavior. A value &lt;= 0
    /// is treated the same as <c>null</c>. Routed through <c>Benzene.Core.Middleware</c>'s
    /// <c>BoundedFanOut</c>.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
}

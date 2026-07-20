using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Diagnostics;

/// <summary>
/// Opt-in startup diagnostic for middleware that silently no-ops when wired in the wrong order.
/// Mirrors <c>Benzene.ResponseEvents</c>' unmapped-response diagnostic and <c>Benzene.Clients</c>'
/// <c>ValidateOutboundRouting</c> - call once after building a pipeline, decide what to do with the
/// findings. <b>Advisory, never throws.</b>
/// </summary>
/// <remarks>
/// Only the unambiguous rule is checked: <c>UseW3CTraceContext()</c> must be the first middleware in
/// the pipeline it's added to, because everything after it inherits the ambient <c>Activity.Current</c>
/// parent - anything before it starts a new, disconnected trace and loses inbound <c>traceparent</c>
/// continuation. This is checked against the exact builder the middleware was added to, so it has no
/// false-positive surface.
/// <para>
/// The related "enrichment needs <c>UseBenzeneInvocation()</c> upstream" rule is deliberately not
/// checked here: the batch/per-message transports (SQS, SNS, Kafka, Event Hub) auto-wire
/// <c>UseBenzeneInvocation</c> inside a per-message <em>sub</em>-pipeline - a different builder than
/// the one visible at configuration time - so a check on the visible builder would warn on exactly
/// the transports where <c>invocationId</c> is correctly populated. See the "Diagnosing Failures"
/// doc's ordering-footguns section for that rule.
/// </para>
/// </remarks>
public static class PipelineOrderingDiagnosticsExtensions
{
    private const string W3CTraceContextName = "W3CTraceContext";

    /// <summary>
    /// Inspects the ordered middleware in <paramref name="builder"/> and returns any ordering issues.
    /// Reads each middleware's <see cref="IMiddleware{TContext}.Name"/> by resolving it against
    /// <paramref name="serviceResolver"/>, skipping (never failing on) any middleware that can't be
    /// resolved for inspection.
    /// </summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="serviceResolver">Resolver used to materialize each middleware to read its name.</param>
    /// <param name="builder">The pipeline builder to inspect. Returns empty for a builder type whose ordered items can't be read.</param>
    /// <returns>The ordering issues found, in pipeline order; empty when there are none.</returns>
    public static PipelineOrderingIssue[] FindPipelineOrderingIssues<TContext>(
        this IServiceResolver serviceResolver, IMiddlewarePipelineBuilder<TContext> builder)
    {
        if (builder is not MiddlewarePipelineBuilder<TContext> concrete)
        {
            return Array.Empty<PipelineOrderingIssue>();
        }

        var items = concrete.GetItems();
        var names = new string?[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            try
            {
                names[i] = items[i](serviceResolver)?.Name;
            }
            catch
            {
                // A middleware that can't be resolved just for name inspection (e.g. a constructor
                // dependency isn't available at check time) is skipped - the check is advisory and
                // must never be the thing that fails startup.
                names[i] = null;
            }
        }

        var issues = new List<PipelineOrderingIssue>();

        var w3cIndex = Array.IndexOf(names, W3CTraceContextName);
        if (w3cIndex > 0)
        {
            issues.Add(new PipelineOrderingIssue(W3CTraceContextName, w3cIndex,
                $"{W3CTraceContextName} is at position {w3cIndex} but must be the first middleware in its " +
                "pipeline - middleware added before it starts a new trace and won't continue an inbound " +
                "traceparent. Move .UseW3CTraceContext() to the top of this pipeline."));
        }

        return issues.ToArray();
    }

    /// <summary>
    /// Runs <see cref="FindPipelineOrderingIssues{TContext}"/> and logs each issue as a warning,
    /// returning the findings so the caller can act further (e.g. fail a startup check in CI). A
    /// no-op with no findings.
    /// </summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="serviceResolver">Resolves each middleware for inspection and (if <paramref name="logger"/> is null) an <see cref="ILogger"/>.</param>
    /// <param name="builder">The pipeline builder to inspect.</param>
    /// <param name="logger">Logger to warn on; resolved from DI when null.</param>
    /// <returns>The issues found (also logged).</returns>
    public static PipelineOrderingIssue[] LogPipelineOrderingIssues<TContext>(
        this IServiceResolver serviceResolver, IMiddlewarePipelineBuilder<TContext> builder, ILogger? logger = null)
    {
        var issues = serviceResolver.FindPipelineOrderingIssues(builder);
        if (issues.Length == 0)
        {
            return issues;
        }

        logger ??= serviceResolver.TryGetService<ILoggerFactory>()?.CreateLogger(typeof(PipelineOrderingDiagnosticsExtensions).FullName!);
        if (logger != null)
        {
            foreach (var issue in issues)
            {
                logger.LogWarning("{PipelineOrderingIssue}", issue.Description);
            }
        }

        return issues;
    }
}

/// <summary>
/// A single middleware-ordering issue found by <see cref="PipelineOrderingDiagnosticsExtensions"/>.
/// </summary>
/// <param name="MiddlewareName">The <see cref="IMiddleware{TContext}.Name"/> of the offending middleware.</param>
/// <param name="Index">Its zero-based position in the pipeline.</param>
/// <param name="Description">A human-readable description of the issue and how to fix it.</param>
public record PipelineOrderingIssue(string MiddlewareName, int Index, string Description);

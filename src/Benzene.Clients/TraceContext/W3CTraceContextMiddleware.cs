using System.Diagnostics;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.TraceContext;

/// <summary>
/// Outbound middleware that stamps the current <see cref="Activity"/>'s W3C
/// <c>traceparent</c>/<c>tracestate</c> onto <see cref="OutboundContext.Headers"/>, so the receiving
/// service can continue the same distributed trace. The middleware-pipeline replacement for
/// <see cref="TraceContextBenzeneMessageClient"/> - see
/// <c>work/benzene-clients-redesign-plan.md</c> §2.4.
/// </summary>
public class W3CTraceContextMiddleware : IMiddleware<OutboundContext>
{
    /// <summary>Gets the name of this middleware.</summary>
    public string Name => nameof(W3CTraceContextMiddleware);

    /// <summary>
    /// Stamps the current activity's trace context onto the outbound headers, then continues the pipeline.
    /// </summary>
    /// <param name="context">The outbound context to stamp headers onto.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    public Task HandleAsync(OutboundContext context, Func<Task> next)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            context.Headers["traceparent"] = activity.Id!;
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                context.Headers["tracestate"] = activity.TraceStateString;
            }
        }

        return next();
    }
}

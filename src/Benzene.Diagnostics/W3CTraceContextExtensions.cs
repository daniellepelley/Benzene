using System.Diagnostics;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Diagnostics.Correlation;

namespace Benzene.Diagnostics;

/// <summary>
/// Provides middleware that establishes the current request's <see cref="Activity"/> parent from an
/// inbound W3C <c>traceparent</c>/<c>tracestate</c> header, so distributed traces continue across
/// services instead of starting a new, disconnected trace per hop.
/// </summary>
public static class W3CTraceContextExtensions
{
    /// <summary>
    /// Adds middleware that reads the <c>traceparent</c>/<c>tracestate</c> headers (matched
    /// case-insensitively) and starts the pipeline's root <see cref="Activity"/> with the parsed remote
    /// context as its parent. Falls back to a normal, parentless root span when the headers are missing
    /// or fail to parse.
    /// </summary>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <remarks>
    /// Add this as the FIRST middleware in the pipeline: everything added after it runs with this
    /// <see cref="Activity"/> as the ambient <see cref="Activity.Current"/> parent, so every
    /// automatically-wrapped middleware span (from <c>AddDiagnostics()</c>) correctly nests under the
    /// remote trace. <c>AddDiagnostics()</c>'s own wrapper still creates one extra, harmless span around
    /// this middleware itself (its parent context isn't known until this middleware's body runs) -- that
    /// span has no children and can be ignored in exported traces.
    /// <para>
    /// Only wired for HTTP-based transports today (ASP.NET Core, Azure Functions' ASP.NET-style trigger,
    /// API Gateway) -- SQS/SNS/Kafka/Event Hub inbound extraction is not yet implemented.
    /// </para>
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseW3CTraceContext<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.Use("W3CTraceContext", resolver => async (context, next) =>
        {
            var messageMapper = resolver.GetService<IMessageHeadersGetter<TContext>>();
            var traceparent = messageMapper.GetHeader(context, "traceparent");
            var tracestate = messageMapper.GetHeader(context, "tracestate");

            var activity = !string.IsNullOrEmpty(traceparent) &&
                // isRemote: true - the parent arrived over the wire in an inbound header, so it is a
                // remote parent. The 3-arg overload defaults isRemote to false, which makes
                // ParentBased samplers and OTel exporters treat every ingested trace as an in-process
                // child and mis-decide sampling at each service hop.
                ActivityContext.TryParse(traceparent, string.IsNullOrEmpty(tracestate) ? null : tracestate, isRemote: true, out var parentContext)
                ? BenzeneDiagnostics.ActivitySource.StartActivity("W3CTraceContext.Root", ActivityKind.Server, parentContext)
                : BenzeneDiagnostics.ActivitySource.StartActivity("W3CTraceContext.Root", ActivityKind.Server);

            using (activity)
            {
                await next();
            }
        });
    }
}

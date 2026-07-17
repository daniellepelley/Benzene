using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.TraceContext;

/// <summary>
/// Provides the <see cref="WithW3CTraceContext"/> extension for <see cref="ClientBuilder"/> (the
/// obsolete decorator-chain mechanism), and <see cref="UseW3CTraceContext"/> for an outbound
/// <see cref="OutboundContext"/> pipeline (the current mechanism - see
/// <c>work/benzene-clients-redesign-plan.md</c>).
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Stamps the current <see cref="System.Diagnostics.Activity"/>'s W3C <c>traceparent</c>/<c>tracestate</c>
    /// onto outgoing message headers, so the receiving service can continue the same distributed trace.
    /// </summary>
    /// <param name="source">The client builder to add the decorator to.</param>
    /// <returns>The client builder, for method chaining.</returns>
    public static ClientBuilder WithW3CTraceContext(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new TraceContextBenzeneMessageClientWrapper());
    }

    /// <summary>
    /// Adds <see cref="W3CTraceContextMiddleware"/> to an outbound route pipeline, so every send
    /// through it carries the current distributed trace.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseW3CTraceContext(
        this IMiddlewarePipelineBuilder<OutboundContext> app)
    {
        return app.Use(_ => new W3CTraceContextMiddleware());
    }
}

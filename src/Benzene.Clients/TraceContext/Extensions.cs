using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.TraceContext;

/// <summary>
/// Provides <see cref="UseW3CTraceContext"/> for an outbound <see cref="OutboundContext"/> pipeline -
/// see <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
public static class Extensions
{
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

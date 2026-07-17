using Benzene.Abstractions;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.CorrelationId;

/// <summary>
/// Provides the <see cref="WithCorrelationId"/> extension for <see cref="ClientBuilder"/> (the
/// obsolete decorator-chain mechanism), and <see cref="UseCorrelationId"/> for an outbound
/// <see cref="OutboundContext"/> pipeline (the current mechanism - see
/// <c>work/benzene-clients-redesign-plan.md</c>).
/// </summary>
public static class Extensions
{
    public static ClientBuilder WithCorrelationId(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new CorrelationIdBenzeneMessageClientWrapper());
    }

    /// <summary>
    /// Adds <see cref="CorrelationIdMiddleware"/> to an outbound route pipeline, so every send
    /// through it carries the current correlation ID.
    /// </summary>
    /// <param name="app">The outbound pipeline builder to add the middleware to.</param>
    /// <param name="correlationKey">The header key to stamp the correlation ID onto.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseCorrelationId(
        this IMiddlewarePipelineBuilder<OutboundContext> app, string correlationKey = "correlationId")
    {
        return app.Use(resolver => new CorrelationIdMiddleware(resolver.GetService<ICorrelationId>(), correlationKey));
    }
}

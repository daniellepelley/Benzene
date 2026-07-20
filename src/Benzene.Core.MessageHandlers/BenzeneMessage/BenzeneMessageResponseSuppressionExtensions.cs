using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Pipeline-builder extension for suppressing response serialization on a BenzeneMessage pipeline.
/// </summary>
public static class BenzeneMessageResponseSuppressionExtensions
{
    /// <summary>
    /// Skips writing (serializing) the response for every message on this BenzeneMessage pipeline -
    /// for one-way hosts (Event Hub, Queue Storage, ...) that discard the response, so building it is
    /// wasted work. Call before <c>UseMessageHandlers()</c> so the suppression runs before the router.
    /// Applied automatically by those hosts' <c>UseBenzeneMessage(action)</c>; the request/response
    /// hosts (direct Lambda invoke, HTTP) don't call it and are unaffected.
    /// </summary>
    /// <param name="app">The BenzeneMessage pipeline builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<BenzeneMessageContext> SuppressResponse(this IMiddlewarePipelineBuilder<BenzeneMessageContext> app)
    {
        return app.Use(resolver => new SuppressBenzeneMessageResponseMiddleware(resolver.GetService<BenzeneMessageResponseSuppression>()));
    }
}

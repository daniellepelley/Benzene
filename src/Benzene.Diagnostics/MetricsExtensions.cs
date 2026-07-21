using System.Diagnostics;
using System.Diagnostics.Metrics;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Diagnostics;

/// <summary>
/// Provides extension methods for recording message-level metrics.
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Adds middleware that records <see cref="BenzeneDiagnostics.MessagesProcessed"/> and
    /// <see cref="BenzeneDiagnostics.MessageDuration"/> for each pipeline execution, tagged by topic,
    /// transport, and result.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add metrics to.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// Unlike the automatic per-middleware <see cref="Activity"/> spans from <c>AddDiagnostics()</c>,
    /// this is once-per-message granularity and must be added explicitly around the pipeline stage you
    /// want measured.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneMetrics<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app
            .Use("BenzeneMetrics", resolver => async (context, next) =>
            {
                // Allocation-free timing: a timestamp long, not a heap-allocated Stopwatch per message.
                var startTimestamp = Stopwatch.GetTimestamp();
                // Record in a finally so a throwing pipeline is still counted and timed - an escaped
                // exception is the most important thing to observe, yet without this it was never
                // recorded (the "failure" result tag was only ever reachable for a handler that
                // returned an unsuccessful result WITHOUT throwing).
                var threw = false;
                try
                {
                    await next();
                }
                catch
                {
                    threw = true;
                    throw;
                }
                finally
                {
                    // Unlike Activity, Counter/Histogram have no automatic no-op when nothing is
                    // listening, so gate on Enabled: with no Meter exporter wired this middleware then
                    // costs nothing beyond the timestamp read (no tag build, no DI resolves, no record).
                    if (BenzeneDiagnostics.MessagesProcessed.Enabled || BenzeneDiagnostics.MessageDuration.Enabled)
                    {
                        var result = threw
                            ? "failure"
                            : context is IHasMessageResult { MessageResult: not null } r
                                ? (r.MessageResult.IsSuccessful ? "success" : "failure")
                                : "<missing>";  // no result signal set on a non-throwing completion

                        var tags = new TagList
                        {
                            { "topic", resolver.TryGetService<IMessageGetter<TContext>>()?.GetTopic(context)?.Id ?? "<missing>" },
                            { "transport", resolver.TryGetService<ICurrentTransport>()?.Name ?? "<missing>" },
                            { "result", result },
                        };

                        BenzeneDiagnostics.MessagesProcessed.Add(1, tags);
                        BenzeneDiagnostics.MessageDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds, tags);
                    }
                }
            });
    }
}

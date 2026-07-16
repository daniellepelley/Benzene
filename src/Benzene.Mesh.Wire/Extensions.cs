using System.Diagnostics;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Results;

namespace Benzene.Mesh.Wire;

/// <summary>
/// Pipeline wiring for the mesh wire layer (docs/specification/mesh.md): the reserved descriptor
/// topic (§1) and the trace feed (§3). Each feed is independent and optional per §6 - leaving
/// either call out reduces the mesh, never the service.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Intercepts the reserved <c>mesh</c> topic (plus any <paramref name="aliases"/>) and
    /// short-circuits with <paramref name="descriptor"/>, exactly as health-check interception
    /// works - by topic id alone, ignoring version. Not wiring this in is the "descriptor endpoint
    /// withheld" deployment: every other mesh feed keeps working.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshDescriptor<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, MeshServiceDescriptor descriptor, params string[] aliases)
    {
        var topics = new HashSet<string>(aliases) { MeshTopics.Descriptor };

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("MeshDescriptor", async (context, next) =>
        {
            var messageGetter = resolver.GetService<IMessageGetter<TContext>>();
            var topic = messageGetter.GetTopic(context);
            if (topic?.Id == null || !topics.Contains(topic.Id))
            {
                await next();
                return;
            }

            var resultSetter = resolver.GetService<IMessageHandlerResultSetter<TContext>>();
            await resultSetter.SetResultAsync(context,
                new MessageHandlerResult(topic, MessageHandlerDefinition.Empty(), BenzeneResult.Ok(descriptor)));
        }));
    }

    /// <summary>
    /// Observes every invocation that passes through it and hands the resulting
    /// <see cref="MeshTraceEvent"/> to <paramref name="exporter"/> after downstream middleware
    /// finishes. Wire it outermost (before health-check/descriptor interception) so it sees every
    /// invocation. Because the message handler layer already converts a missing handler, a request
    /// conversion failure, and a handler exception into results, every routed invocation produces a
    /// status - trace coverage is structural.
    ///
    /// An incoming W3C <c>traceparent</c> header joins the existing trace; a missing or malformed
    /// one starts a fresh trace. <see cref="MeshSpan.Current"/> carries the span across the
    /// invocation so handlers can propagate it outbound. A null <paramref name="exporter"/> is a
    /// pass-through (the trace feed is simply off), a throwing exporter loses its own event and
    /// never the response, and a transport without an <paramref name="statusReader"/> (see
    /// <see cref="IMeshStatusReader{TContext}"/>) records an empty status - the §6 degradation
    /// rule, end to end.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshTrace<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        MeshServiceInfo info,
        IMeshTraceExporter? exporter,
        IMeshStatusReader<TContext>? statusReader = null)
    {
        if (exporter == null)
        {
            return app;
        }

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("MeshTrace", async (context, next) =>
        {
            var messageGetter = resolver.GetService<IMessageGetter<TContext>>();
            var topic = messageGetter.GetTopic(context);
            var headers = messageGetter.GetHeaders(context);

            headers.TryGetValue("traceparent", out var traceparent);
            if (!Traceparent.TryParse(traceparent, out var traceId, out var parentSpanId))
            {
                traceId = Traceparent.NewId(16);
                parentSpanId = string.Empty;
            }

            var traceEvent = new MeshTraceEvent
            {
                TraceId = traceId,
                SpanId = Traceparent.NewId(8),
                ParentSpanId = string.IsNullOrEmpty(parentSpanId) ? null : parentSpanId,
                Service = string.IsNullOrEmpty(info.Service) ? null : info.Service,
                InstanceId = info.InstanceId,
                Topic = topic?.Id ?? string.Empty,
                TopicVersion = string.IsNullOrEmpty(topic?.Version) ? null : topic!.Version,
                StartedAt = DateTimeOffset.UtcNow,
                CorrelationId = headers.TryGetValue("x-correlation-id", out var correlationId) ? correlationId : null
            };

            var stopwatch = Stopwatch.StartNew();
            var previousSpan = MeshSpan.Current;
            MeshSpan.Current = new MeshSpan(traceEvent.TraceId, traceEvent.SpanId);
            try
            {
                await next();
            }
            finally
            {
                MeshSpan.Current = previousSpan;
                traceEvent.DurationMs = stopwatch.Elapsed.TotalMilliseconds;
                traceEvent.Status = statusReader?.GetStatus(context) ?? string.Empty;
                try
                {
                    exporter.Export(traceEvent);
                }
                catch
                {
                    // a broken exporter loses its own event, never the caller's response (spec §6)
                }
            }
        }));
    }
}

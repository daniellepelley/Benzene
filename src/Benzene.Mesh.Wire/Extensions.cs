using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
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

            TryGetHeader(headers, "traceparent", out var traceparent);
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
                CorrelationId = TryGetHeader(headers, "x-correlation-id", out var correlationId) ? correlationId : null
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
                // The failure's WHY (spec §3 exceptionType, optional): the handler layer converts thrown
                // exceptions into results, recording the type in the scoped MessageErrorState — read it
                // back here so the push-plane TraceEvent carries it too (type only, never message/stack).
                traceEvent.ExceptionType = resolver.TryGetService<MessageErrorState>()?.ExceptionTypeName;
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

    /// <summary>
    /// Observes every invocation and reports FAILURES to <paramref name="exporter"/> as deduplicated
    /// issue occurrences (spec §4.1) — the pipeline-native "what is wrong" feed. Wire it immediately
    /// INSIDE <see cref="UseMeshTrace{TContext}"/> (trace outermost, issues next): inside the trace
    /// middleware, <see cref="MeshSpan.Current"/> still names the current invocation's trace, which
    /// becomes the issue's exemplar trace id; without <c>UseMeshTrace</c> the exemplars are simply
    /// empty (degradation-legal). A null <paramref name="exporter"/> is a pass-through (the feed is
    /// off). Without a <paramref name="statusReader"/> result-status failures cannot be observed and
    /// only propagating exceptions are reported. The success path is deliberately near-free: one
    /// (memoized) status read and one set-membership test, no allocation.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshIssues<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        MeshServiceInfo info,
        IMeshIssueExporter? exporter,
        IMeshStatusReader<TContext>? statusReader = null)
    {
        if (exporter == null)
        {
            return app;
        }

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("MeshIssues", async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                // Host shutdown/drain redelivery is not an issue — without this guard every deploy
                // would file a phantom exception issue per in-flight message (the ExceptionHandler
                // middleware's own rule, mirrored).
                throw;
            }
            catch (Exception ex)
            {
                // A propagating exception IS the failure — report, then rethrow untouched.
                Report(resolver, context, "exception", ex.GetType().FullName, null);
                throw;
            }

            if (statusReader == null)
            {
                return; // no outcome to read on this transport — only propagating exceptions report
            }
            var status = statusReader.GetStatus(context) ?? string.Empty;
            if (!MeshStatusClass.IsSuccess(status))
            {
                var errorState = resolver.TryGetService<MessageErrorState>();
                Report(resolver, context, status, errorState?.ExceptionTypeName, errorState?.Hint);
            }
        }));

        void Report(IServiceResolver resolver, TContext context, string status, string? exceptionType, string? hint)
        {
            try
            {
                var topic = resolver.GetService<IMessageGetter<TContext>>().GetTopic(context);
                var transport = resolver.TryGetService<ICurrentTransport>();
                exporter!.Export(new MeshIssueOccurrence
                {
                    Service = info.Service,
                    Topic = topic?.Id ?? string.Empty,
                    Version = string.IsNullOrEmpty(topic?.Version) ? null : topic!.Version,
                    Transport = transport != null && transport.Name != TransportNames.Unresolved ? transport.Name : null,
                    Status = status,
                    ExceptionType = exceptionType,
                    ResolutionHint = hint,
                    TraceId = MeshSpan.Current?.TraceId,
                    At = DateTimeOffset.UtcNow
                });
            }
            catch
            {
                // no mesh feed may ever fail the invocation it observed (spec §6)
            }
        }
    }

    // Header keys are case-insensitive on read regardless of the envelope dictionary's comparer
    // (wire-contracts.md §2). The envelope headers deserialize into an ordinal Dictionary, so a
    // cross-language participant (e.g. a Go service whose net/http canonicalizes "traceparent" to
    // "Traceparent") would otherwise be missed and its trace not joined.
    private static bool TryGetHeader(IDictionary<string, string> headers, string key, out string? value)
    {
        if (headers.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = header.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}

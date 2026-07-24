using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Diagnostics.Correlation;

namespace Benzene.Diagnostics;

public class ActivityMiddlewareDecorator<TContext> : IMiddleware<TContext>
{
    private readonly IMiddleware<TContext> _inner;
    private readonly IServiceResolver _serviceResolver;

    public ActivityMiddlewareDecorator(IMiddleware<TContext> inner, IServiceResolver serviceResolver)
    {
        _inner = inner;
        _serviceResolver = serviceResolver;
    }

    public string Name => _inner.Name;

    public Task HandleAsync(TContext context, Func<Task> next)
    {
        var activity = BenzeneDiagnostics.ActivitySource.StartActivity(Name);
        if (activity is null)
        {
            // Nothing is listening (no exporter/listener wired), so there is no span to record. Return
            // the inner middleware's task directly - no tag work, no try/catch, and crucially no async
            // state machine allocated for this stage. This is what makes AddDiagnostics genuinely
            // (not just "effectively") free per stage when tracing isn't being exported.
            return _inner.HandleAsync(context, next);
        }

        return HandleTracedAsync(activity, context, next);
    }

    private async Task HandleTracedAsync(Activity activity, TContext context, Func<Task> next)
    {
        using (activity)
        {
            // Only the topic-bearing span carries benzene.status, so a trace-backed mesh reader
            // (Benzene.Mesh.Fleet.*) can reconstruct MeshTraceEvent.Status from the same span it reads
            // the topic off - see work/otel-fleet-adapter-scope.md §6a.
            var taggedTopic = Tag(activity, context);

            try
            {
                await _inner.HandleAsync(context, next);

                // Set after next() so the handler's result is available. Recorded as the real Benzene
                // wire status (e.g. "ok"/"not-found") - the trace's success/failure signal - on the
                // topic-bearing span only, avoiding a duplicate tag on every wrapped stage.
                if (taggedTopic && context is IHasMessageResult { MessageResult: not null } result
                    && !string.IsNullOrEmpty(result.MessageResult.Status))
                {
                    activity.SetTag("benzene.status", result.MessageResult.Status);
                }
            }
            catch (Exception ex)
            {
                // Without this, a span that threw looks identical to one that succeeded in a trace
                // viewer (Jaeger/Tempo/App Insights) - no error flag, no exception. Marking the span
                // (at every level the exception propagates through) is what lets a trace point at the
                // failing stage. Then rethrow untouched - this only observes, it doesn't handle.
                activity.AddException(ex);
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);

                // An escaped exception has no BenzeneResult; record it as its own status (matching the
                // metric's "exception" token) so the trace shows a failure, not a missing status.
                if (taggedTopic)
                {
                    activity.SetTag("benzene.status", "exception");
                }

                throw;
            }
        }
    }

    /// <summary>Tags the span; returns true when a real topic was resolved and tagged (so the caller
    /// knows this is a topic-bearing span, the one that also carries benzene.status).</summary>
    private bool Tag(Activity activity, TContext context)
    {
        // Skip the transport tag until a transport pipeline has actually resolved one - in a
        // multi-transport function the outer probe stages run before resolution, so tagging here would
        // stamp the "<missing>" sentinel on every pass-through span (noise in a trace viewer).
        var transport = _serviceResolver.TryGetService<ICurrentTransport>();
        if (transport is not null && transport.Name != TransportNames.Unresolved)
        {
            activity.SetTag("benzene.transport", transport.Name);
        }

        // The message-identity tags (topic/version/handler/correlation-id, and the after-next()
        // benzene.status the caller keys off this return value) belong on ONE span per dispatch. For a
        // transport whose topic is intrinsic to the message (HTTP route, BenzeneMessage envelope),
        // GetTopic resolves at every stage, so without a guard every wrapped span would carry them - and
        // a trace-backed mesh reader (Benzene.Mesh.Fleet.*) emits one flow event per topic-tagged span,
        // counting a single message once per middleware stage. The scoped ActivityTopicTagState is one
        // shared instance across every stage of this message's DI scope, so the FIRST topic-bearing span
        // claims the tags and every later stage returns false here. If the guard isn't registered
        // (TryGetService returns null) we fall back to the original per-stage behaviour.
        var tagState = _serviceResolver.TryGetService<ActivityTopicTagState>();
        if (tagState is { Tagged: true })
        {
            return false;
        }

        var getter = _serviceResolver.TryGetService<IMessageGetter<TContext>>();
        var topic = getter?.GetTopic(context);
        if (topic is not null && !string.IsNullOrEmpty(topic.Id))
        {
            if (tagState is not null)
            {
                tagState.Tagged = true;
            }

            activity.SetTag("benzene.topic", topic.Id);
            activity.SetTag("benzene.version", topic.Version);

            var handler = _serviceResolver.TryGetService<IMessageHandlerDefinitionLookUp>()?.FindHandler(topic);
            if (handler is not null)
            {
                activity.SetTag("benzene.handler", handler.HandlerType.Name);
            }

            // Only when the message actually carried a business correlation id (x-correlation-id) - the
            // same source and null-when-absent rule as MeshTraceEvent.CorrelationId, so the mesh never
            // fabricates one. This is what makes mesh:query:correlation answerable from a trace store: a
            // trace-backed reader (Benzene.Mesh.Fleet.*) searches this tag - see
            // work/otel-fleet-adapter-scope.md §6b. On the topic-bearing span only, like benzene.status.
            var correlationId = getter!.GetHeader(context, "x-correlation-id");
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("benzene.correlation-id", correlationId);
            }

            return true;
        }

        return false;
    }
}

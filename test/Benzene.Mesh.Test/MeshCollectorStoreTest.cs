using Benzene.Mesh.Collector;
using Benzene.Mesh.Wire;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// Store behaviors the conformance sequences don't pin: the bounded ring window (eviction, with
/// cumulative stats deliberately outliving it) and the fleet flow-list cap.
/// </summary>
public class MeshCollectorStoreTest
{
    private static MeshTraceEvent Event(string traceId, string spanId, string service, string topic,
        DateTimeOffset startedAt, string status = "ok")
    {
        return new MeshTraceEvent
        {
            TraceId = traceId,
            SpanId = spanId,
            Service = service,
            Topic = topic,
            Status = status,
            DurationMs = 1,
            StartedAt = startedAt
        };
    }

    [Fact]
    public void AddEvents_EventWithNullStatus_IsAcceptedAndCountedAsFailure()
    {
        // A wire payload can deserialize "status": null into an actual null (nullable-reference
        // annotations are not enforced at runtime). The §6 degradation rule requires ingestion to
        // accept it rather than throw ArgumentNullException on the null status-count key.
        var store = new MeshCollectorStore();
        var evt = Event("trace-1", "span-1", "svc", "topic", DateTimeOffset.UtcNow, status: null!);

        var accepted = store.AddEvents(new[] { evt });

        Assert.Equal(1, accepted);
        var topic = store.Topic("topic", null);
        Assert.NotNull(topic);
        Assert.Equal(1, topic!.Invocations);
        Assert.Equal(1, topic.Errors);
    }

    [Fact]
    public void RingEviction_DropsTheWindowButKeepsCumulativeStats()
    {
        var store = new MeshCollectorStore(maxTraceEvents: 2);
        var now = DateTimeOffset.UtcNow;

        store.AddEvents(new[]
        {
            Event("trace-1", "span-1", "svc", "topic", now),
            Event("trace-1", "span-2", "svc", "topic", now.AddMilliseconds(1))
        });
        Assert.NotNull(store.Trace("trace-1"));

        store.AddEvents(new[]
        {
            Event("trace-2", "span-3", "svc", "topic", now.AddMilliseconds(2)),
            Event("trace-2", "span-4", "svc", "topic", now.AddMilliseconds(3))
        });

        Assert.Null(store.Trace("trace-1")); // aged out of the bounded window
        var topic = store.Topic("topic", null);
        Assert.NotNull(topic);
        Assert.Equal(4, topic!.Invocations); // cumulative stats outlive the ring
    }

    [Fact]
    public void FleetFlowList_IsCappedAtTwentyNewestFirst()
    {
        var store = new MeshCollectorStore();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 25; i++)
        {
            store.AddEvents(new[] { Event($"trace-{i}", $"span-{i}", "svc", "topic", now.AddSeconds(i)) });
        }

        var fleet = store.Fleet();

        Assert.Equal(20, fleet.Traces.Count);
        Assert.True(fleet.Traces[0].StartedAt > fleet.Traces[^1].StartedAt); // newest first
    }

    [Fact]
    public void Consumers_AreDerivedAtQueryTimeFromParentage()
    {
        var store = new MeshCollectorStore();
        var now = DateTimeOffset.UtcNow;
        var parent = Event("trace-1", "span-parent", "caller", "outer", now);
        var child = Event("trace-1", "span-child", "callee", "inner", now.AddMilliseconds(1));
        child.ParentSpanId = "span-parent";
        var selfCall = Event("trace-2", "span-self", "callee", "inner", now.AddMilliseconds(2));
        selfCall.ParentSpanId = "span-child"; // same-service parent: no edge

        store.AddEvents(new[] { parent, child, selfCall });

        var inner = store.Topic("inner", null);
        Assert.NotNull(inner);
        Assert.Equal(new List<string> { "caller" }, inner!.Consumers);
    }

    // ---- correlation lookup (mesh:query:correlation, mesh-product-owner ruling 2026-07-23) ----

    private static MeshTraceEvent CorrEvent(string traceId, string spanId, string service, string topic,
        DateTimeOffset startedAt, string? correlationId, string status = "ok")
    {
        var evt = Event(traceId, spanId, service, topic, startedAt, status);
        evt.CorrelationId = correlationId;
        return evt;
    }

    [Fact]
    public void Correlation_GroupsMatchingFlowsByTrace_OrderedByEarliestStart_EventsInStartOrder()
    {
        // One business correlation id spans two distinct traces; a third trace carries a different id.
        var store = new MeshCollectorStore();
        var now = DateTimeOffset.UtcNow;
        store.AddEvents(new[]
        {
            // trace-b starts later but its events are added first, to prove ordering is by StartedAt.
            CorrEvent("trace-b", "b2", "shipping", "book", now.AddSeconds(10).AddMilliseconds(5), "corr-1"),
            CorrEvent("trace-b", "b1", "orders", "ship", now.AddSeconds(10), "corr-1"),
            CorrEvent("trace-a", "a1", "orders", "create", now, "corr-1"),
            CorrEvent("trace-a", "a2", "payments", "capture", now.AddMilliseconds(5), "corr-1", status: "service-unavailable"),
            CorrEvent("trace-c", "c1", "orders", "create", now.AddSeconds(20), "other"),
        });

        var view = store.Correlation("corr-1");

        Assert.NotNull(view);
        Assert.Equal("corr-1", view!.CorrelationId);
        Assert.Equal(2, view.Traces.Count);
        // Traces ordered by earliest event start: trace-a (now) before trace-b (now+10s).
        Assert.Equal("trace-a", view.Traces[0].TraceId);
        Assert.Equal("trace-b", view.Traces[1].TraceId);
        // Events within a trace in start order (b1 before b2 despite reversed insertion).
        Assert.Equal(new[] { "a1", "a2" }, view.Traces[0].Events.Select(e => e.SpanId).ToArray());
        Assert.Equal(new[] { "b1", "b2" }, view.Traces[1].Events.Select(e => e.SpanId).ToArray());
        // The per-leg service/topic/status the owner wants to read survives intact.
        Assert.Equal("payments", view.Traces[0].Events[1].Service);
        Assert.Equal("service-unavailable", view.Traces[0].Events[1].Status);
    }

    [Fact]
    public void Correlation_ExcludesNullCorrelationEvents_AndReturnsNullWhenNothingMatches()
    {
        // The mesh never fabricates a correlation id: a flow whose entry set no x-correlation-id
        // header simply won't appear in any lookup.
        var store = new MeshCollectorStore();
        var now = DateTimeOffset.UtcNow;
        store.AddEvents(new[]
        {
            CorrEvent("trace-1", "s1", "orders", "create", now, correlationId: null),
        });

        Assert.Null(store.Correlation("corr-1"));
    }

    [Fact]
    public async Task CorrelationQueryHandler_EmptyId_BadRequest_UnknownId_NotFound_KnownId_Ok()
    {
        var store = new MeshCollectorStore();
        store.AddEvents(new[] { CorrEvent("trace-1", "s1", "orders", "create", DateTimeOffset.UtcNow, "corr-1") });
        var handler = new CorrelationQueryMessageHandler(store);

        Assert.Equal("bad-request", (await handler.HandleAsync(new CorrelationQuery { CorrelationId = "" })).Status);
        Assert.Equal("not-found", (await handler.HandleAsync(new CorrelationQuery { CorrelationId = "nope" })).Status);
        var ok = await handler.HandleAsync(new CorrelationQuery { CorrelationId = "corr-1" });
        Assert.Equal("ok", ok.Status);
        Assert.Equal("corr-1", ok.Payload.CorrelationId);
        Assert.Single(ok.Payload.Traces);
    }
}

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
        DateTimeOffset startedAt, string status = "Ok")
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
}

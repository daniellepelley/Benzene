using Benzene.Mesh.Collector;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The backend-composed fleet read model: topic stats from the usage feed (CloudWatch/App Insights),
/// recent flows + anonymous-but-live services from the trace source (X-Ray). Verifies the result-tag
/// error vocabulary, the absent-dimension markers (per-service stats, topic duration → "missingFeeds"),
/// and fetch isolation (one failing source never blanks the whole view).
/// </summary>
public class CompositeMeshFleetReadModelTest
{
    private sealed class FakeTraceSource : IMeshTraceSource
    {
        public IReadOnlyList<TraceSummary> RecentFlows { get; init; } = new List<TraceSummary>();
        public TraceView? Trace { get; init; }
        public CorrelationView? Correlation { get; init; }
        public bool ThrowOnRecent { get; init; }

        public MeshTimeRange? LastRecentRange { get; private set; }

        public Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
            => Task.FromResult(Trace);
        public Task<CorrelationView?> GetCorrelationAsync(string correlationId, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Correlation);
        public Task<IReadOnlyList<TraceSummary>> GetRecentFlowsAsync(int limit = 20, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
        {
            LastRecentRange = range;
            return ThrowOnRecent ? throw new InvalidOperationException("trace backend down") : Task.FromResult(RecentFlows);
        }
    }

    private sealed class FakeUsageSource : IMeshUsageSource
    {
        public MeshUsage? Usage { get; init; }
        public bool Throw { get; init; }

        public Task<MeshUsage?> FetchUsageAsync(CancellationToken cancellationToken = default)
            => Throw ? throw new InvalidOperationException("usage backend down") : Task.FromResult(Usage);
    }

    private static MeshUsageEntry Entry(string topic, string? status, long count, double? avgMs = null) =>
        new(topic, version: null, service: null, transport: "sqs", status: status, count: count, avgDurationMs: avgMs,
            source: MeshUsageSource.CloudWatch);

    private static MeshUsage Usage(params MeshUsageEntry[] entries) =>
        new(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, entries);

    [Fact]
    public async Task FleetAsync_BuildsTopics_FromUsage_WithResultVocabularyErrorRule()
    {
        var usage = new FakeUsageSource
        {
            Usage = Usage(
                Entry("orders:create", "success", 10),
                Entry("orders:create", "not-found", 3),   // itemized failure status
                Entry("orders:create", "exception", 2),   // error bucket, NOT a wire status
                Entry("orders:create", "failure", 1),      // error bucket, no status
                Entry("orders:create", "<missing>", 4))    // no outcome → not an error
        };
        var model = new CompositeMeshFleetReadModel(new FakeTraceSource(), new[] { usage });

        var fleet = await model.FleetAsync();

        var topic = Assert.Single(fleet.Topics);
        Assert.Equal("orders:create", topic.Topic);
        Assert.Equal(20, topic.Invocations);                 // all counts
        Assert.Equal(6, topic.Errors);                       // not-found(3) + exception(2) + failure(1); NOT success/<missing>
        Assert.Equal(10, topic.StatusCounts["success"]);
        Assert.Equal(2, topic.StatusCounts["exception"]);
        Assert.Equal(4, topic.StatusCounts["<missing>"]);    // raw token kept verbatim
        // CloudWatch supplies no descriptor and no duration → both marked absent (UI renders "—").
        Assert.Contains("descriptor", topic.MissingFeeds);
        Assert.Contains("duration", topic.MissingFeeds);
        Assert.Equal(0, topic.AvgDurationMs);
    }

    [Fact]
    public async Task FleetAsync_UsesDuration_WhenAUsageSourceMeasuresIt()
    {
        var usage = new FakeUsageSource
        {
            Usage = Usage(Entry("orders:get", "success", 2, avgMs: 10), Entry("orders:get", "success", 8, avgMs: 20))
        };
        var model = new CompositeMeshFleetReadModel(new FakeTraceSource(), new[] { usage });

        var topic = Assert.Single((await model.FleetAsync()).Topics);

        Assert.Equal(18, topic.AvgDurationMs, 3);            // count-weighted: (2*10 + 8*20)/10
        Assert.DoesNotContain("duration", topic.MissingFeeds); // measured → not marked absent
    }

    [Fact]
    public async Task FleetAsync_BuildsAnonymousServices_FromRecentFlows_StatsAbsent()
    {
        var traces = new FakeTraceSource
        {
            RecentFlows = new List<TraceSummary>
            {
                new() { TraceId = "t1", Services = new List<string> { "orders-api", "billing-api" }, Failed = true },
                new() { TraceId = "t2", Services = new List<string> { "orders-api" } }
            }
        };
        var model = new CompositeMeshFleetReadModel(traces, Array.Empty<IMeshUsageSource>());

        var fleet = await model.FleetAsync();

        Assert.Equal(2, fleet.Traces.Count);                 // recent flows surfaced verbatim
        Assert.Equal(2, fleet.Services.Count);               // distinct services across flows
        var orders = Assert.Single(fleet.Services, s => s.Service == "orders-api");
        Assert.Equal(MeshHealth.Unknown, orders.Health);
        // Known only from traffic: no descriptor, no health feed, and no per-service counts.
        Assert.Contains("descriptor", orders.MissingFeeds);
        Assert.Contains("health", orders.MissingFeeds);
        Assert.Contains("stats", orders.MissingFeeds);
        Assert.Equal(0, orders.Invocations);
    }

    [Fact]
    public async Task FleetAsync_FailingUsageSource_LeavesTracesIntact()
    {
        var traces = new FakeTraceSource
        {
            RecentFlows = new List<TraceSummary> { new() { TraceId = "t1", Services = new List<string> { "svc" } } }
        };
        var model = new CompositeMeshFleetReadModel(traces, new IMeshUsageSource[] { new FakeUsageSource { Throw = true } });

        var fleet = await model.FleetAsync();

        Assert.Empty(fleet.Topics);          // the bad usage source degrades to no topics...
        Assert.Single(fleet.Traces);         // ...without blanking the trace-sourced slice
        Assert.Single(fleet.Services);
    }

    [Fact]
    public async Task FleetAsync_FailingTraceSource_LeavesTopicsIntact()
    {
        var usage = new FakeUsageSource { Usage = Usage(Entry("orders:create", "success", 5)) };
        var model = new CompositeMeshFleetReadModel(new FakeTraceSource { ThrowOnRecent = true }, new[] { usage });

        var fleet = await model.FleetAsync();

        Assert.Single(fleet.Topics);         // topics survive...
        Assert.Empty(fleet.Traces);          // ...while the failing trace slice degrades to empty
        Assert.Empty(fleet.Services);
    }

    [Fact]
    public async Task TraceAndCorrelation_DelegateToTheTraceSource_WhileServiceAndTopicAreOmitted()
    {
        var traces = new FakeTraceSource
        {
            Trace = new TraceView { TraceId = "t1" },
            Correlation = new CorrelationView { CorrelationId = "c1" }
        };
        var model = new CompositeMeshFleetReadModel(traces, Array.Empty<IMeshUsageSource>());

        Assert.Equal("t1", (await model.TraceAsync("t1"))!.TraceId);
        Assert.Equal("c1", (await model.CorrelationAsync("c1"))!.CorrelationId);
        Assert.Null(await model.ServiceAsync("orders-api")); // no descriptor feed on this plane
        Assert.Null(await model.TopicAsync("orders:create", null));
    }
}

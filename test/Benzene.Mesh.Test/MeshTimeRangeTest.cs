using Benzene.Mesh.Collector;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Wire;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// Phase D time-range (docs/mesh-ui.md "Time range", src/Benzene.Mesh.Collector/CLAUDE.md). Pins the
/// Grafana-grammar resolver, the push-collector plane's flows-honor-window / counts-cumulative-since-start
/// self-description, and the composite plane threading the range to its trace source while reporting the
/// usage feed's own window. All additive: a null range must reproduce today's behavior (no Window field).
/// </summary>
public class MeshTimeRangeTest
{
    private static readonly DateTimeOffset Now = new(2026, 07, 24, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("now", 0)]
    [InlineData("now-5m", -5 * 60)]
    [InlineData("now-1h", -60 * 60)]
    [InlineData("now-24h", -24 * 60 * 60)]
    [InlineData("now-7d", -7 * 24 * 60 * 60)]
    [InlineData("now-1d/d", -24 * 60 * 60)]  // trailing rounding suffix accepted and ignored
    public void Resolve_GrafanaRelative_ResolvesAgainstNow(string from, int expectedOffsetSeconds)
    {
        var window = MeshTimeRangeResolver.Resolve(new MeshTimeRange { From = from }, Now);

        Assert.NotNull(window);
        Assert.Equal(Now.AddSeconds(expectedOffsetSeconds), window!.Value.From);
        Assert.Equal(Now, window.Value.To); // null To ⇒ now
    }

    [Fact]
    public void Resolve_AbsoluteIso_IsUsedVerbatim()
    {
        var window = MeshTimeRangeResolver.Resolve(
            new MeshTimeRange { From = "2026-07-24T10:00:00Z", To = "2026-07-24T11:00:00Z" }, Now);

        Assert.NotNull(window);
        Assert.Equal(new DateTimeOffset(2026, 07, 24, 10, 0, 0, TimeSpan.Zero), window!.Value.From);
        Assert.Equal(new DateTimeOffset(2026, 07, 24, 11, 0, 0, TimeSpan.Zero), window.Value.To);
    }

    [Theory]
    [InlineData(null)]                       // no range at all
    [InlineData("")]                         // no lower bound
    [InlineData("garbage")]                  // unparseable lower bound
    public void Resolve_NoLowerBound_IsUnfiltered(string? from)
    {
        var range = from == null ? null : new MeshTimeRange { From = from };
        Assert.Null(MeshTimeRangeResolver.Resolve(range, Now));
    }

    private static MeshTraceEvent Event(string traceId, DateTimeOffset startedAt, string? correlationId = null) =>
        new()
        {
            TraceId = traceId,
            SpanId = traceId + "-s",
            Service = "svc",
            Topic = "orders:create",
            Status = "ok",
            DurationMs = 1,
            StartedAt = startedAt,
            CorrelationId = correlationId
        };

    [Fact]
    public void Fleet_WithWindow_FiltersFlowsButBadgesCountsAsCumulative()
    {
        var store = new MeshCollectorStore();
        store.AddEvents(new[]
        {
            Event("old", Now.AddHours(-3)),   // outside a 1h window
            Event("recent", Now.AddMinutes(-10)) // inside
        });

        // now-1h relative to a fixed "now" the test controls by feeding absolute bounds around it.
        var range = new MeshTimeRange { From = Now.AddHours(-1).ToString("O"), To = Now.AddMinutes(1).ToString("O") };
        var fleet = store.Fleet(range);

        // Only the in-window flow survives; the cumulative topic count still reflects BOTH events.
        Assert.Single(fleet.Traces);
        Assert.Equal("recent", fleet.Traces[0].TraceId);
        Assert.Equal(2, fleet.Topics.Single().Invocations);

        // The window self-describes: flows honored [From,To], counts are cumulative-since-start.
        Assert.NotNull(fleet.Window);
        Assert.False(fleet.Window!.CountsWindowed);
        Assert.NotNull(fleet.Window.CountsSince); // the collector's start
    }

    [Fact]
    public void Fleet_WithoutWindow_OmitsWindow_AndReturnsTodaysFlows()
    {
        var store = new MeshCollectorStore();
        store.AddEvents(new[] { Event("a", Now.AddHours(-3)), Event("b", Now.AddMinutes(-10)) });

        var fleet = store.Fleet();

        Assert.Null(fleet.Window);        // absent field = today's shape, fixtures/old clients untouched
        Assert.Equal(2, fleet.Traces.Count); // unfiltered
    }

    [Fact]
    public void Correlation_WithWindow_FiltersByTraceStart_AndReportsWindow()
    {
        var store = new MeshCollectorStore();
        store.AddEvents(new[]
        {
            Event("old", Now.AddHours(-3), correlationId: "corr"),
            Event("recent", Now.AddMinutes(-10), correlationId: "corr")
        });

        var range = new MeshTimeRange { From = Now.AddHours(-1).ToString("O"), To = Now.AddMinutes(1).ToString("O") };
        var view = store.Correlation("corr", range);

        Assert.NotNull(view);
        Assert.Single(view!.Traces);
        Assert.Equal("recent", view.Traces[0].TraceId);
        Assert.NotNull(view.Window);
        Assert.False(view.Window!.CountsWindowed);
    }

    [Fact]
    public void Topic_StandaloneResponse_CarriesWindow_ButEmbeddedInFleetDoesNot()
    {
        var store = new MeshCollectorStore();
        store.AddEvents(new[] { Event("t", Now.AddMinutes(-10)) });
        var range = new MeshTimeRange { From = Now.AddHours(-1).ToString("O") };

        var standalone = store.Topic("orders:create", null, range);
        Assert.NotNull(standalone!.Window); // the :topic response carries it

        var embedded = store.Fleet(range).Topics.Single();
        Assert.Null(embedded.Window); // the fleet's one Window covers the view; the row's stays null
    }

    private sealed class WindowCapturingTraceSource : IMeshTraceSource
    {
        public MeshTimeRange? LastRange { get; private set; }
        public Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
            => Task.FromResult<TraceView?>(null);
        public Task<CorrelationView?> GetCorrelationAsync(string correlationId, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
            => Task.FromResult<CorrelationView?>(null);
        public Task<IReadOnlyList<TraceSummary>> GetRecentFlowsAsync(int limit = 20, MeshTimeRange? range = null, CancellationToken cancellationToken = default)
        {
            LastRange = range;
            return Task.FromResult<IReadOnlyList<TraceSummary>>(new List<TraceSummary>());
        }
    }

    // A usage source that ignores the requested window and always reports its own fixed one — the
    // "can't be sub-windowed" case (like the cumulative collector feed).
    private sealed class FixedUsageSource : IMeshUsageSource
    {
        private readonly MeshUsage _usage;
        public FixedUsageSource(MeshUsage usage) => _usage = usage;
        public Task<MeshUsage?> FetchUsageAsync(MeshUsageWindow? window = null, CancellationToken cancellationToken = default)
            => Task.FromResult<MeshUsage?>(_usage);
    }

    // A usage source that honors the requested window — it queries over exactly those bounds and echoes them
    // back (the CloudWatch/App-Insights behavior after the count-windowing fast-follow).
    private sealed class WindowHonoringUsageSource : IMeshUsageSource
    {
        public Task<MeshUsage?> FetchUsageAsync(MeshUsageWindow? window = null, CancellationToken cancellationToken = default)
        {
            var start = window?.FromUtc ?? DateTimeOffset.UtcNow.AddHours(-24);
            var end = window?.ToUtc ?? DateTimeOffset.UtcNow;
            var entry = new MeshUsageEntry("orders:create", null, null, "sqs", "success", 5, null, MeshUsageSource.CloudWatch);
            return Task.FromResult<MeshUsage?>(new MeshUsage(DateTimeOffset.UtcNow, start, end, new[] { entry }));
        }
    }

    [Fact]
    public async Task Composite_ThreadsRangeToTraceSource_AndReportsUsageFeedWindow()
    {
        var traceSource = new WindowCapturingTraceSource();
        var usageStart = new DateTimeOffset(2026, 07, 23, 12, 0, 0, TimeSpan.Zero); // the usage feed's own baked window
        var usage = new MeshUsage(Now, usageStart, Now,
            new[] { new MeshUsageEntry("orders:create", null, null, "sqs", "success", 5, null, MeshUsageSource.CloudWatch) });
        var model = new CompositeMeshFleetReadModel(traceSource, new IMeshUsageSource[] { new FixedUsageSource(usage) });

        var range = new MeshTimeRange { From = "now-1h" };
        var fleet = await model.FleetAsync(range);

        // The picked range reached the trace source (flows honor it)...
        Assert.Same(range, traceSource.LastRange);
        // ...but the composite reports counts as NOT windowed, covering the usage feed's own window.
        Assert.NotNull(fleet.Window);
        Assert.False(fleet.Window!.CountsWindowed);
        Assert.Equal(MeshTimeRangeResolver.ToIso(usageStart), fleet.Window.CountsSince);
    }

    [Fact]
    public async Task Composite_WhenEveryUsageSourceHonorsTheWindow_CountsAreWindowed()
    {
        // Every usage source echoes the requested window → the counts really cover [from,to] → CountsWindowed=true.
        var model = new CompositeMeshFleetReadModel(new WindowCapturingTraceSource(),
            new IMeshUsageSource[] { new WindowHonoringUsageSource() });

        var fleet = await model.FleetAsync(new MeshTimeRange { From = "now-1h" });

        Assert.NotNull(fleet.Window);
        Assert.True(fleet.Window!.CountsWindowed);
        Assert.Null(fleet.Window.CountsSince); // no "cumulative from" caveat — the counts honor the range
    }

    [Fact]
    public async Task Composite_WhenAnyUsageSourceCannotWindow_CountsStayCumulative()
    {
        // One honoring source + one that can't (a fixed/cumulative window) → the union isn't windowed → false.
        var cumulative = new MeshUsage(Now, Now.AddDays(-30), Now,
            new[] { new MeshUsageEntry("orders:create", null, null, "sqs", "success", 3, null, MeshUsageSource.Collector) });
        var model = new CompositeMeshFleetReadModel(new WindowCapturingTraceSource(),
            new IMeshUsageSource[] { new WindowHonoringUsageSource(), new FixedUsageSource(cumulative) });

        var fleet = await model.FleetAsync(new MeshTimeRange { From = "now-1h" });

        Assert.NotNull(fleet.Window);
        Assert.False(fleet.Window!.CountsWindowed);
        Assert.NotNull(fleet.Window.CountsSince); // surfaces the earliest window the counts actually cover
    }
}

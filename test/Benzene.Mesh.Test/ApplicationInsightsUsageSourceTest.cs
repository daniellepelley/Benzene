using Benzene.Mesh.Contracts;
using Benzene.Mesh.Usage.ApplicationInsights;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The Application Insights usage adapter: grouped (topic, transport, result) counts from the backend
/// become usage.json entries at exactly those dimensions - no version/service/duration (the
/// missing-dimension degradation path), zero-count and topic-less rows dropped, never a guessed count.
/// </summary>
public class ApplicationInsightsUsageSourceTest
{
    private static ApplicationInsightsUsageSource Source(params UsageCount[] rows)
    {
        var query = new Mock<IApplicationInsightsUsageQuery>();
        query.Setup(x => x.QueryAsync(It.IsAny<ApplicationInsightsUsageOptions>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        return new ApplicationInsightsUsageSource(query.Object, new ApplicationInsightsUsageOptions("ws-guid", TimeSpan.FromHours(6)));
    }

    [Fact]
    public async Task FetchUsageAsync_MapsEachGroupedRow_ToAUsageEntry()
    {
        var usage = await Source(
            new UsageCount("orders:create", "sqs", "success", 15),
            new UsageCount("orders:create", "sqs", "failure", 2),
            new UsageCount("orders:get", "http", "success", 0)) // in the table but no traffic → dropped
            .FetchUsageAsync();

        Assert.NotNull(usage);
        Assert.Equal(2, usage!.Entries.Length);

        var success = Assert.Single(usage.Entries, e => e.Status == "success");
        Assert.Equal("orders:create", success.Topic);
        Assert.Equal("sqs", success.Transport);
        Assert.Equal(15, success.Count);
        Assert.Equal(MeshUsageSource.ApplicationInsights, success.Source);
        Assert.Null(success.Version);
        Assert.Null(success.Service);
        Assert.Null(success.AvgDurationMs);

        Assert.Single(usage.Entries, e => e.Status == "failure" && e.Count == 2);

        Assert.NotNull(usage.WindowStartUtc);
        Assert.NotNull(usage.WindowEndUtc);
        Assert.Equal(TimeSpan.FromHours(6), usage.WindowEndUtc!.Value - usage.WindowStartUtc!.Value);
    }

    [Fact]
    public async Task FetchUsageAsync_NoRows_ReportsAWiredFeedWithNoEntries()
    {
        var usage = await Source().FetchUsageAsync();

        Assert.NotNull(usage);
        Assert.Empty(usage!.Entries);
    }

    [Fact]
    public async Task FetchUsageAsync_RowWithoutATopic_IsSkipped()
    {
        // A grouped row whose topic dimension was absent (null) is never guessed into an entry.
        var usage = await Source(new UsageCount(null, "sqs", "success", 99)).FetchUsageAsync();

        Assert.Empty(usage!.Entries);
    }

    [Fact]
    public async Task FetchUsageAsync_PreservesAbsentTransportAndStatus_AsNull()
    {
        // The backend can group by a dimension it doesn't carry → null; surfaced as absent, not "".
        var usage = await Source(new UsageCount("orders:create", null, null, 4)).FetchUsageAsync();

        var entry = Assert.Single(usage!.Entries);
        Assert.Equal(4, entry.Count);
        Assert.Null(entry.Transport);
        Assert.Null(entry.Status);
    }
}

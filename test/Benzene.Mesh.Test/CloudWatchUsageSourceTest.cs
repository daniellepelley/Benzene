using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Usage.CloudWatch;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The CloudWatch usage adapter: list the metric's live dimension combinations, sum each over the window,
/// and map to <c>usage.json</c> entries at exactly the dimensions the counter carries (topic, transport,
/// status) - no version/service/duration (the missing-dimension degradation path), never a guessed count.
/// </summary>
public class CloudWatchUsageSourceTest
{
    private static Metric Metric(string? topic, string? transport, string? result)
    {
        var dimensions = new List<Dimension>();
        if (topic != null) dimensions.Add(new Dimension { Name = "topic", Value = topic });
        if (transport != null) dimensions.Add(new Dimension { Name = "transport", Value = transport });
        if (result != null) dimensions.Add(new Dimension { Name = "result", Value = result });
        return new Metric { Namespace = "Benzene/Mesh", MetricName = "benzene.messages.processed", Dimensions = dimensions };
    }

    // A mock CloudWatch whose ListMetrics returns the given metrics and whose GetMetricData returns, for
    // each query, the datapoints `counts` maps to that query's topic dimension (empty list = no traffic).
    private static Mock<IAmazonCloudWatch> CloudWatch(List<Metric> metrics, Func<Metric, List<double>> valuesFor)
    {
        var mock = new Mock<IAmazonCloudWatch>();
        mock.Setup(x => x.ListMetricsAsync(It.IsAny<ListMetricsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListMetricsResponse { Metrics = metrics });
        mock.Setup(x => x.GetMetricDataAsync(It.IsAny<GetMetricDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetMetricDataRequest req, CancellationToken _) => new GetMetricDataResponse
            {
                MetricDataResults = req.MetricDataQueries
                    .Select(q => new MetricDataResult { Id = q.Id, Values = valuesFor(q.MetricStat.Metric) })
                    .ToList()
            });
        return mock;
    }

    private static List<double> ValuesFor(Metric metric)
    {
        var topic = metric.Dimensions.First(d => d.Name == "topic").Value;
        var result = metric.Dimensions.First(d => d.Name == "result").Value;
        return (topic, result) switch
        {
            ("orders:create", "success") => new List<double> { 10, 5 }, // summed across buckets = 15
            ("orders:create", "failure") => new List<double> { 2 },
            ("orders:get", "success") => new List<double>(),            // in ListMetrics but no traffic this window
            _ => new List<double>()
        };
    }

    [Fact]
    public async Task FetchUsageAsync_SumsEachDimensionCombination_OverTheWindow()
    {
        var metrics = new List<Metric>
        {
            Metric("orders:create", "sqs", "success"),
            Metric("orders:create", "sqs", "failure"),
            Metric("orders:get", "http", "success"),
        };
        var options = new CloudWatchUsageOptions(timeWindow: TimeSpan.FromHours(6));
        var source = new CloudWatchUsageSource(CloudWatch(metrics, ValuesFor).Object, options);

        var usage = await source.FetchUsageAsync();

        Assert.NotNull(usage);
        // The zero-traffic combination is dropped (in ListMetrics, but not used in this window).
        Assert.Equal(2, usage!.Entries.Length);

        var success = Assert.Single(usage.Entries, e => e.Status == "success");
        Assert.Equal("orders:create", success.Topic);
        Assert.Equal("sqs", success.Transport);
        Assert.Equal(15, success.Count);
        Assert.Equal(MeshUsageSource.CloudWatch, success.Source);
        // The counter carries none of these - reported absent, never guessed.
        Assert.Null(success.Version);
        Assert.Null(success.Service);
        Assert.Null(success.AvgDurationMs);

        Assert.Single(usage.Entries, e => e.Status == "failure" && e.Count == 2);

        // The window the UI shows spans the configured lookback.
        Assert.NotNull(usage.WindowStartUtc);
        Assert.NotNull(usage.WindowEndUtc);
        Assert.Equal(TimeSpan.FromHours(6), usage.WindowEndUtc!.Value - usage.WindowStartUtc!.Value);
    }

    [Fact]
    public async Task FetchUsageAsync_NoMetrics_ReportsAWiredFeedWithNoEntries()
    {
        // Never null: the metric simply hasn't been seen → "feed wired, no traffic observed" (empty
        // entries), which the aggregator still publishes - distinct from no usage feed at all.
        var source = new CloudWatchUsageSource(CloudWatch(new List<Metric>(), _ => new List<double>()).Object,
            new CloudWatchUsageOptions());

        var usage = await source.FetchUsageAsync();

        Assert.NotNull(usage);
        Assert.Empty(usage!.Entries);
    }

    [Fact]
    public async Task FetchUsageAsync_MetricWithoutATopicDimension_IsSkipped()
    {
        // A stray metric in the namespace that isn't ours (no topic) is never guessed into an entry.
        var metrics = new List<Metric>
        {
            new Metric { Dimensions = new List<Dimension> { new Dimension { Name = "transport", Value = "sqs" } } }
        };
        var source = new CloudWatchUsageSource(
            CloudWatch(metrics, _ => new List<double> { 99 }).Object, new CloudWatchUsageOptions());

        var usage = await source.FetchUsageAsync();

        Assert.Empty(usage!.Entries);
    }
}

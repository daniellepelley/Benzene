using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

namespace Benzene.Mesh.Usage.ApplicationInsights;

/// <summary>
/// The default <see cref="IApplicationInsightsUsageQuery"/>: issues a KQL query against a Log Analytics
/// workspace (the store behind a workspace-based Application Insights resource), summing the
/// <c>customMetrics</c> counter by its <c>customDimensions</c>. Returns one <see cref="UsageCount"/> per
/// (topic, transport, result) combination over the window.
/// </summary>
public class LogsQueryUsageQuery : IApplicationInsightsUsageQuery
{
    private readonly LogsQueryClient _client;

    /// <summary>Initializes a new instance wrapping <paramref name="client"/>.</summary>
    /// <param name="client">The Azure Monitor logs-query client (authenticated for the workspace).</param>
    public LogsQueryUsageQuery(LogsQueryClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsageCount>> QueryAsync(
        ApplicationInsightsUsageOptions options, TimeSpan window, CancellationToken cancellationToken = default)
    {
        // Sum the counter (customMetrics.valueSum is the per-interval sum; with delta temporality that is
        // the request delta, so a sum over the window is the total) grouped by the three tag dimensions.
        // The dimension keys are configurable; the result columns are fixed so parsing stays stable.
        var kql =
            $"customMetrics\n" +
            $"| where name == \"{options.MetricName}\"\n" +
            $"| extend _topic = tostring(customDimensions[\"{options.TopicDimension}\"]),\n" +
            $"         _transport = tostring(customDimensions[\"{options.TransportDimension}\"]),\n" +
            $"         _result = tostring(customDimensions[\"{options.ResultDimension}\"])\n" +
            $"| summarize _count = sum(valueSum) by _topic, _transport, _result";

        var response = await _client.QueryWorkspaceAsync(
            options.WorkspaceId, kql, new QueryTimeRange(window), cancellationToken: cancellationToken);

        var table = response.Value.Table;
        var rows = new List<UsageCount>(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            var count = (long)Math.Round(row.GetDouble("_count") ?? 0d);
            rows.Add(new UsageCount(
                Topic: NullIfEmpty(row.GetString("_topic")),
                Transport: NullIfEmpty(row.GetString("_transport")),
                Result: NullIfEmpty(row.GetString("_result")),
                Count: count));
        }

        return rows;
    }

    // KQL tostring of a missing customDimensions key yields an empty string; treat that as an absent
    // dimension (null), never a real "" value - the same missing-dimension honesty the feed requires.
    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}

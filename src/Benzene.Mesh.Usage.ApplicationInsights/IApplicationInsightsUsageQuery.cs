namespace Benzene.Mesh.Usage.ApplicationInsights;

/// <summary>
/// One grouped usage count read from the metrics backend: the summed count at a
/// (topic, transport, result) combination. A thin, backend-agnostic row so
/// <see cref="ApplicationInsightsUsageSource"/> is unit-testable without constructing Azure SDK models.
/// Any dimension the backend didn't carry is <c>null</c>.
/// </summary>
public record UsageCount(string? Topic, string? Transport, string? Result, long Count);

/// <summary>
/// The query seam <see cref="ApplicationInsightsUsageSource"/> depends on: runs the grouped
/// count-by-dimension query against the metrics backend and returns plain <see cref="UsageCount"/> rows.
/// The default implementation (<see cref="LogsQueryUsageQuery"/>) issues KQL against a Log Analytics
/// workspace; tests substitute a fake so the source's mapping/window/degradation logic is covered
/// without a live Azure dependency. (Azure's <c>LogsQueryClient</c> is a concrete class, not an
/// interface, so this small seam is the mockable equivalent of the CloudWatch adapter's
/// <c>IAmazonCloudWatch</c>.)
/// </summary>
public interface IApplicationInsightsUsageQuery
{
    /// <summary>Runs the grouped count query over the absolute <c>[<paramref name="startUtc"/>, <paramref name="endUtc"/>]</c>
    /// window and returns one row per dimension combination.</summary>
    Task<IReadOnlyList<UsageCount>> QueryAsync(
        ApplicationInsightsUsageOptions options, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default);
}

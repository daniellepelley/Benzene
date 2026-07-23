namespace Benzene.Mesh.Fleet.Aws.XRay;

/// <summary>
/// Tuning for <see cref="XRayTraceSource"/>. Only correlation search needs these — a trace lookup is by
/// id (<c>BatchGetTraces</c>, no window). X-Ray's <c>GetTraceSummaries</c> requires a time range, so a
/// correlation lookup searches the last <see cref="CorrelationLookback"/> for traces carrying the
/// correlation-id annotation.
/// </summary>
public class XRayTraceSourceOptions
{
    /// <summary>How far back a <c>mesh:query:correlation</c> search scans X-Ray for matching traces.
    /// Default 24 hours — a business correlation id (a ticket/log id) is typically chased soon after the
    /// event, and X-Ray retains traces for 30 days so a longer window is available if you widen it.</summary>
    public TimeSpan CorrelationLookback { get; init; } = TimeSpan.FromHours(24);
}

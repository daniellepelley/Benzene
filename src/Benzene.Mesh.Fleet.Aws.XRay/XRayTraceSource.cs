using Amazon.XRay;
using Amazon.XRay.Model;
using Benzene.Mesh.Collector;

namespace Benzene.Mesh.Fleet.Aws.XRay;

/// <summary>
/// An <see cref="IMeshTraceSource"/> that answers <c>mesh:query:trace</c> from AWS X-Ray: it fetches
/// the trace's segments with <c>BatchGetTraces</c> and maps the topic-bearing spans into a
/// <see cref="TraceView"/> (see <see cref="XRaySegmentMapper"/>). This is the AWS realisation of the
/// trace-backed fleet reader scoped in <c>work/otel-fleet-adapter-scope.md</c> - the fleet UI's trace
/// waterfall over an existing observability backend, no push collector required.
/// </summary>
/// <remarks>
/// Trace stats and service health are deliberately not sourced here (see <see cref="IMeshTraceSource"/>);
/// X-Ray traces are sampled, so counts would be biased, and X-Ray has no heartbeat feed. Those compose
/// from an <c>IMeshUsageSource</c> (CloudWatch) and the heartbeat plane in later increments.
/// </remarks>
public class XRayTraceSource : IMeshTraceSource
{
    private readonly IAmazonXRay _xray;

    /// <summary>Creates the source over an X-Ray client (region/credentials come from the client).</summary>
    public XRayTraceSource(IAmazonXRay xray) => _xray = xray;

    /// <summary>Fetches the trace's segments from X-Ray and maps its topic-bearing spans into a
    /// <see cref="TraceView"/>, or null when X-Ray has no such trace or it carried no Benzene spans.</summary>
    public async Task<TraceView?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(traceId))
        {
            return null;
        }

        var response = await _xray.BatchGetTracesAsync(
            new BatchGetTracesRequest { TraceIds = new List<string> { traceId } }, cancellationToken);

        var trace = response.Traces?.FirstOrDefault(t => t.Id == traceId)
                    ?? response.Traces?.FirstOrDefault();

        // X-Ray returns the id in UnprocessedTraceIds (not Traces) when it has no such trace: a null
        // rather than an empty view, so the query handler answers NotFound rather than an empty waterfall.
        if (trace?.Segments is not { Count: > 0 } segments)
        {
            return null;
        }

        var events = XRaySegmentMapper.Map(traceId, segments.Select(s => s.Document));
        if (events.Count == 0)
        {
            // A real X-Ray trace with no Benzene topic-bearing span is not a mesh flow - answer NotFound
            // rather than an empty waterfall the UI would render as a zero-event trace.
            return null;
        }

        return new TraceView { TraceId = traceId, Events = events };
    }
}

using System.Text.Json;
using Benzene.Results;

namespace Benzene.Mesh.Wire;

/// <summary>
/// One observed failure occurrence, as the issue middleware reports it — raw fields, no identity.
/// Classification and the normative fingerprint are computed where occurrences are accumulated
/// (<see cref="HttpMeshIssueExporter"/>), keeping the middleware dumb and letting a test exporter
/// see raw occurrences.
/// </summary>
public class MeshIssueOccurrence
{
    public string Service { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Transport { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? ResolutionHint { get; set; }
    public string? TraceId { get; set; }
    public DateTimeOffset At { get; set; }
}

/// <summary>
/// Receives every failure occurrence the issue middleware produces. Implementations MUST be
/// non-blocking and lossy under backpressure (spec §4/§4.1 sender rules), and MUST deduplicate at
/// source — per-occurrence events never reach the wire.
/// </summary>
public interface IMeshIssueExporter
{
    void Export(MeshIssueOccurrence occurrence);
}

/// <summary>
/// Accumulates occurrences into fingerprint-deduplicated <see cref="MeshIssue"/>s (spec §4.1) and
/// POSTs them to a collector's wire-envelope endpoint as <c>mesh:issues</c> batches on an interval.
/// Counts on the wire are DELTAS (occurrences since the previous flush). An EMPTY batch is still
/// sent each interval — the feed's liveness assertion, letting a collector distinguish a quiet
/// wired service from an unwired one. Lossy by design in every failure mode: the accumulator is
/// bounded (drop-new when full), a failed send drops its deltas, and <see cref="DisposeAsync"/>
/// flushes the tail.
/// </summary>
public sealed class HttpMeshIssueExporter : IMeshIssueExporter, IAsyncDisposable, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _envelopeUrl;
    private readonly string _service;
    private readonly int _maxFingerprints;
    private readonly TimeSpan _flushInterval;
    private readonly object _lock = new();
    private readonly Dictionary<string, MeshIssue> _accumulator = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly Task _pump;
    private int _disposed;

    public HttpMeshIssueExporter(
        HttpClient httpClient,
        string envelopeUrl,
        string service,
        TimeSpan? flushInterval = null,
        int maxFingerprints = 256)
    {
        _httpClient = httpClient;
        _envelopeUrl = envelopeUrl;
        _service = service;
        _maxFingerprints = maxFingerprints;
        // Issues aren't latency-sensitive; a longer interval than traces keeps the liveness cost tiny.
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(30);
        _pump = Task.Run(PumpAsync);
    }

    public void Export(MeshIssueOccurrence occurrence)
    {
        var classification = MeshIssueClassification.Classify(occurrence.Status, occurrence.ExceptionType);
        var fingerprint = MeshIssueFingerprint.Compute(
            occurrence.Service, occurrence.Topic, occurrence.Version, classification,
            occurrence.ExceptionType, occurrence.Status);

        lock (_lock)
        {
            if (!_accumulator.TryGetValue(fingerprint, out var issue))
            {
                if (_accumulator.Count >= _maxFingerprints)
                {
                    return; // bounded: drop-new, lossy by design (spec §4 sender rules)
                }
                issue = new MeshIssue
                {
                    Fingerprint = fingerprint,
                    Classification = classification,
                    Service = occurrence.Service,
                    Topic = occurrence.Topic,
                    Version = occurrence.Version,
                    FirstSeen = occurrence.At
                };
                _accumulator[fingerprint] = issue;
            }

            issue.Count++;
            issue.LastSeen = occurrence.At >= issue.LastSeen ? occurrence.At : issue.LastSeen;
            issue.FirstSeen = occurrence.At < issue.FirstSeen ? occurrence.At : issue.FirstSeen;
            // Scalar detail fields latest-wins; exemplars keep the NEWEST ≤3 (recent ones are the
            // ones still inside a trace backend's retention).
            issue.Transport = occurrence.Transport ?? issue.Transport;
            issue.Status = occurrence.Status;
            issue.ExceptionType = occurrence.ExceptionType ?? issue.ExceptionType;
            issue.ResolutionHint = occurrence.ResolutionHint ?? issue.ResolutionHint;
            if (!string.IsNullOrEmpty(occurrence.TraceId) && !issue.ExemplarTraceIds.Contains(occurrence.TraceId!))
            {
                issue.ExemplarTraceIds.Add(occurrence.TraceId!);
                if (issue.ExemplarTraceIds.Count > 3)
                {
                    issue.ExemplarTraceIds.RemoveAt(0);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _pump.ConfigureAwait(false);
            return;
        }
        _stopping.Cancel();
        await _pump.ConfigureAwait(false);
        _stopping.Dispose();
    }

    /// <summary>Synchronous disposal bridge with a bounded wait — same rationale as
    /// <see cref="HttpMeshTraceExporter.Dispose"/> (an unreachable collector must not hang shutdown;
    /// the feed is lossy by design).</summary>
    public void Dispose()
    {
        try
        {
            DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // send failures are already swallowed; nothing actionable on shutdown
        }
    }

    private async Task PumpAsync()
    {
        while (!_stopping.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_flushInterval, _stopping.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            await FlushAsync().ConfigureAwait(false);
        }
        await FlushAsync().ConfigureAwait(false); // tail flush on shutdown
    }

    private async Task FlushAsync()
    {
        List<MeshIssue> issues;
        lock (_lock)
        {
            issues = _accumulator.Values.ToList();
            _accumulator.Clear(); // counts are deltas: each flush starts a fresh window
        }
        try
        {
            var envelope = new
            {
                topic = MeshTopics.Issues,
                headers = new Dictionary<string, string>(),
                // An empty batch is deliberate: the liveness assertion ("feed alive, nothing failing").
                body = MeshJson.Serialize(new MeshIssueBatch { Service = _service, Issues = issues })
            };
            using var content = new StringContent(JsonSerializer.Serialize(envelope), System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_envelopeUrl, content).ConfigureAwait(false);
            _ = response.StatusCode; // a rejected batch is dropped, not retried — lossy by design
        }
        catch
        {
            // an unreachable collector reduces the mesh, never the service; the dropped deltas are lost
        }
    }
}

/// <summary>The wire-contracts §3 success class, for the issue middleware's failure test. Mirrors the
/// collector's own classifier (an unknown or empty status counts as a failure).</summary>
internal static class MeshStatusClass
{
    private static readonly HashSet<string> SuccessStatuses = new()
    {
        BenzeneResultStatus.Ok,
        BenzeneResultStatus.Created,
        BenzeneResultStatus.Accepted,
        BenzeneResultStatus.Updated,
        BenzeneResultStatus.Deleted,
        BenzeneResultStatus.Ignored
    };

    public static bool IsSuccess(string? status) => status != null && SuccessStatuses.Contains(status);
}

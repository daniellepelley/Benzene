using System.Text.Json;
using System.Threading.Channels;

namespace Benzene.Mesh.Wire;

/// <summary>
/// Receives every trace event the mesh trace middleware produces. Implementations MUST be
/// non-blocking and lossy under backpressure (docs/specification/mesh.md §4's sender rules): no
/// mesh feed may ever fail, slow, or block the invocation it observed - the middleware
/// additionally shields the invocation from a throwing exporter.
/// </summary>
public interface IMeshTraceExporter
{
    void Export(MeshTraceEvent traceEvent);
}

/// <summary>
/// Batches trace events and POSTs them to a mesh collector's wire-envelope endpoint as
/// <c>mesh:traces</c> messages, from a single background task. Lossy by design in every failure
/// mode, per spec §4: a full buffer drops the new event, a failed send drops the batch, and
/// <see cref="DisposeAsync"/> flushes the tail so shutdown doesn't lose it. Works against any
/// envelope-speaking collector - including the Go reference collector (meshd).
/// </summary>
public sealed class HttpMeshTraceExporter : IMeshTraceExporter, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _envelopeUrl;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private readonly Channel<MeshTraceEvent> _queue;
    private readonly CancellationTokenSource _stopping = new();
    private readonly Task _pump;
    private int _disposed;

    public HttpMeshTraceExporter(
        HttpClient httpClient,
        string envelopeUrl,
        int batchSize = 64,
        TimeSpan? flushInterval = null,
        int bufferSize = 1024)
    {
        _httpClient = httpClient;
        _envelopeUrl = envelopeUrl;
        _batchSize = batchSize;
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(5);
        _queue = Channel.CreateBounded<MeshTraceEvent>(new BoundedChannelOptions(bufferSize)
        {
            FullMode = BoundedChannelFullMode.DropWrite, // full buffer drops the new event
            SingleReader = true
        });
        _pump = Task.Run(PumpAsync);
    }

    public void Export(MeshTraceEvent traceEvent)
    {
        _queue.Writer.TryWrite(traceEvent); // non-blocking; false means dropped, lossy by design
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _pump.ConfigureAwait(false); // idempotent: later disposals just await the flush
            return;
        }
        _queue.Writer.TryComplete();
        _stopping.Cancel();
        await _pump.ConfigureAwait(false);
        _stopping.Dispose();
    }

    private async Task PumpAsync()
    {
        var batch = new List<MeshTraceEvent>(_batchSize);
        while (true)
        {
            var flushDeadline = false;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
                timeout.CancelAfter(_flushInterval);
                if (!await _queue.Reader.WaitToReadAsync(timeout.Token).ConfigureAwait(false))
                {
                    break; // writer completed: flush and exit
                }
            }
            catch (OperationCanceledException)
            {
                flushDeadline = true; // interval elapsed (or stopping): flush what we have
            }

            while (batch.Count < _batchSize && _queue.Reader.TryRead(out var traceEvent))
            {
                batch.Add(traceEvent);
            }
            if (batch.Count >= _batchSize || flushDeadline || _stopping.IsCancellationRequested)
            {
                await FlushAsync(batch).ConfigureAwait(false);
            }
            if (_stopping.IsCancellationRequested && !_queue.Reader.TryPeek(out _))
            {
                break;
            }
        }

        while (_queue.Reader.TryRead(out var remaining))
        {
            batch.Add(remaining);
        }
        await FlushAsync(batch).ConfigureAwait(false);
    }

    private async Task FlushAsync(List<MeshTraceEvent> batch)
    {
        if (batch.Count == 0)
        {
            return;
        }
        try
        {
            var envelope = new
            {
                topic = MeshTopics.Traces,
                headers = new Dictionary<string, string>(),
                body = MeshJson.Serialize(new MeshTraceBatch { Events = batch.ToList() })
            };
            using var content = new StringContent(JsonSerializer.Serialize(envelope), System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_envelopeUrl, content).ConfigureAwait(false);
            _ = response.StatusCode; // a rejected batch is dropped, not retried - lossy by design
        }
        catch
        {
            // an unreachable collector reduces the mesh, never the service
        }
        batch.Clear();
    }
}

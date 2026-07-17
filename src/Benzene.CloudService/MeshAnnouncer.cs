using System.Text;
using System.Text.Json;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Wire;

namespace Benzene.CloudService;

/// <summary>
/// The outbound announcement half of the mesh feeds (mesh.md §4–§5): registers the service's
/// descriptor with the collector on startup (retrying until the collector is up), then heartbeats
/// on an interval, carrying the aggregated health check response and the descriptor hash.
///
/// The §6 degradation rule applies throughout: every failure - collector down, send faulted,
/// health check throwing - is swallowed and retried on the next tick, and nothing here ever runs
/// on, blocks, or fails an invocation. Started at wire-up when the descriptor is available
/// eagerly, otherwise by the first invocation (see <see cref="EnsureStarted"/>).
/// </summary>
internal sealed class MeshAnnouncer : IAsyncDisposable
{
    private static readonly HttpClient DefaultHttp = new();

    private readonly MeshServiceInfo _info;
    private readonly CloudServiceDescriptorSource _descriptorSource;
    private readonly string _collectorEnvelopeUrl;
    private readonly IHealthCheck[] _healthChecks;
    private readonly HttpClient _http;
    private readonly TimeSpan _heartbeatInterval;
    private readonly CancellationTokenSource _stopping = new();
    private int _started;

    public MeshAnnouncer(
        MeshServiceInfo info,
        CloudServiceDescriptorSource descriptorSource,
        string collectorEnvelopeUrl,
        IEnumerable<IHealthCheck> healthChecks,
        HttpClient? http = null,
        TimeSpan? heartbeatInterval = null)
    {
        _info = info;
        _descriptorSource = descriptorSource;
        _collectorEnvelopeUrl = collectorEnvelopeUrl;
        _healthChecks = healthChecks.ToArray();
        _http = http ?? DefaultHttp;
        _heartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Starts the announce loop exactly once; safe to call from every invocation. The
    /// <paramref name="resolver"/> is used only if the descriptor still needs deriving from the
    /// registry (the lazy path); pass null when the descriptor was built eagerly.
    /// </summary>
    public void EnsureStarted(IServiceResolver? resolver)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        var descriptor = resolver == null ? _descriptorSource.TryGet() : _descriptorSource.Get(resolver);
        if (descriptor == null)
        {
            // No descriptor available yet (lazy path started without a resolver) - let the next
            // invocation try again rather than announcing an empty contract.
            Interlocked.Exchange(ref _started, 0);
            return;
        }

        _ = Task.Run(() => RunAsync(descriptor, _stopping.Token));
    }

    private async Task RunAsync(MeshServiceDescriptor descriptor, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await SendAsync(MeshTopics.Register, MeshJson.Serialize(descriptor)))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await SendAsync(MeshTopics.Heartbeat, MeshJson.Serialize(new MeshHeartbeat
                {
                    Service = _info.Service,
                    InstanceId = _info.InstanceId,
                    DescriptorHash = descriptor.DescriptorHash,
                    SentAt = DateTimeOffset.UtcNow,
                    Health = await GetHealthAsync()
                }));
                await Task.Delay(_heartbeatInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // stopping - nothing to clean up; a collector notices via missing heartbeats (spec §6)
        }
    }

    private async Task<HealthCheckResponse> GetHealthAsync()
    {
        try
        {
            var result = await HealthCheckProcessor.PerformHealthChecksAsync(
                HealthChecks.Constants.DefaultHealthCheckTopic, _healthChecks);
            return result is IBenzeneResult<HealthCheckResponse> typed
                ? typed.Payload
                : new HealthCheckResponse(result.IsSuccessful, new Dictionary<string, HealthCheckResult>());
        }
        catch
        {
            // a broken check must degrade the heartbeat's payload, never stop the heartbeats
            return new HealthCheckResponse(false, new Dictionary<string, HealthCheckResult>());
        }
    }

    private async Task<bool> SendAsync(string topic, string body)
    {
        try
        {
            var envelope = JsonSerializer.Serialize(
                new { topic, headers = new Dictionary<string, string>(), body });
            using var content = new StringContent(envelope, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(_collectorEnvelopeUrl, content, _stopping.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // collector down: reduced mesh, never a service failure (spec §6)
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _stopping.Dispose();
    }
}

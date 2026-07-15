using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Benzene.Mesh.Host;

/// <summary>
/// Triggers a full <see cref="MeshAggregator.RunOnceAsync"/> pass on a timer
/// (<see cref="MeshHostConfig.PollIntervalSeconds"/>) - needed because a bare Docker Compose
/// deployment has no external scheduler (unlike a hosted deployment, where <c>mesh:aggregate</c> is
/// typically triggered by a scheduled Lambda/Function invocation instead). This is new capability
/// local to this Host app only - <see cref="MeshAggregateMessageHandler"/> itself stays
/// invocation-triggered-only, unchanged.
/// </summary>
public class MeshPollBackgroundService : BackgroundService
{
    private readonly MeshAggregator _aggregator;
    private readonly MeshServiceRegistry _registry;
    private readonly TimeSpan _interval;
    private readonly ILogger<MeshPollBackgroundService> _logger;

    /// <summary>Initializes a new instance of the <see cref="MeshPollBackgroundService"/> class.</summary>
    /// <param name="aggregator">Runs each aggregation pass.</param>
    /// <param name="registry">The services to poll each pass.</param>
    /// <param name="config">Supplies <see cref="MeshHostConfig.PollIntervalSeconds"/>.</param>
    /// <param name="logger">Logs a failed pass - a failure here must not crash the host.</param>
    public MeshPollBackgroundService(MeshAggregator aggregator, MeshServiceRegistry registry, MeshHostConfig config, ILogger<MeshPollBackgroundService> logger)
    {
        _aggregator = aggregator;
        _registry = registry;
        _interval = TimeSpan.FromSeconds(Math.Max(1, config.PollIntervalSeconds));
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _aggregator.RunOnceAsync(_registry);
            }
            catch (Exception ex)
            {
                // One failed pass must not stop future passes, and must not crash the host - the
                // dashboard simply shows stale data until the next successful pass.
                _logger.LogError(ex, "Mesh aggregation pass failed");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }
}

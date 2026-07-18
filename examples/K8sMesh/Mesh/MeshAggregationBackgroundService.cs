using Microsoft.Extensions.Hosting;

namespace Benzene.Examples.K8sMesh.Mesh;

/// <summary>
/// Runs a discovery + aggregation pass on an interval (the Kubernetes analogue of the AWS
/// EventBridge schedule), so the catalog stays fresh without anyone hitting <c>/mesh/refresh</c>.
/// Failures are swallowed and retried next tick — a transient API/interrogation error must not crash
/// the mesh pod.
/// </summary>
public class MeshAggregationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly MeshAggregationService _aggregation;

    public MeshAggregationBackgroundService(MeshAggregationService aggregation)
    {
        _aggregation = aggregation;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _aggregation.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                // Swallow and retry next tick.
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}

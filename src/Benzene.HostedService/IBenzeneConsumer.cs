namespace Benzene.HostedService;

public interface IBenzeneConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
namespace Benzene.Abstractions.Hosting;

public interface IBenzeneWorker
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
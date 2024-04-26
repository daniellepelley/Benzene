using Benzene.Abstractions.Hosting;

namespace Benzene.SelfHost;

public class CompositeBenzeneWorker : IBenzeneWorker
{
    private readonly IEnumerable<IBenzeneWorker> _workers;
    public CompositeBenzeneWorker(IEnumerable<IBenzeneWorker> workers)
    {
        _workers = workers;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = _workers
            .Select(x => x.StartAsync(cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _workers
            .Select(x => x.StopAsync(cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }
}
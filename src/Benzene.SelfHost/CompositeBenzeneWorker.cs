using Benzene.Abstractions.Hosting;

namespace Benzene.SelfHost;

public class CompositeBenzeneWorker : IBenzeneWorker
{
    private readonly IReadOnlyList<IBenzeneWorker> _workers;
    public CompositeBenzeneWorker(IEnumerable<IBenzeneWorker> workers)
    {
        // Materialize once. Callers pass a deferred query (BenzeneWorkerBuilder.Create hands us
        // `_apps.Select(factory => factory(resolver))`, and every factory news up a fresh worker),
        // so re-enumerating in StopAsync would build a SECOND, never-started worker set and stop
        // those instead of the running ones - silently skipping every worker's drain/close/commit.
        _workers = workers.ToList();
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
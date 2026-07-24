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
        // Start every worker (in parallel), then await them together. If any one fails, roll back the
        // workers that DID start - otherwise a partial composite start leaks their running consume
        // loops / open connections with nothing tracking them (StopAsync is only called on a clean
        // start). Task.WhenAll waits for every task to finish before throwing, so at the catch each
        // task is terminal and IsCompletedSuccessfully reliably identifies the started workers.
        // SafeStart captures a *synchronous* throw as a faulted task, so one worker throwing before
        // its first await still lets the others start (and get rolled back) rather than aborting the
        // materialization mid-way.
        var started = _workers
            .Select(x => (worker: x, task: SafeStart(x, cancellationToken)))
            .ToArray();

        try
        {
            await Task.WhenAll(started.Select(x => x.task));
        }
        catch
        {
            foreach (var (worker, task) in started)
            {
                if (task.IsCompletedSuccessfully)
                {
                    try
                    {
                        await worker.StopAsync(cancellationToken);
                    }
                    catch
                    {
                        // Best-effort rollback: don't let a stop fault mask the original start failure.
                    }
                }
            }

            throw;
        }
    }

    private static Task SafeStart(IBenzeneWorker worker, CancellationToken cancellationToken)
    {
        try
        {
            return worker.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _workers
            .Select(x => x.StopAsync(cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }
}
namespace Benzene.HostedService;

public class CompositeBenzeneConsumer : IBenzeneConsumer
{
    private readonly IEnumerable<IBenzeneConsumer> _consumers;
    public CompositeBenzeneConsumer(IEnumerable<IBenzeneConsumer> consumers)
    {
        _consumers = consumers;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = _consumers
            .Select(x => x.StartAsync(cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _consumers
            .Select(x => x.StopAsync(cancellationToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }
}
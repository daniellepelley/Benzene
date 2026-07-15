using Benzene.Abstractions.Hosting;
using Benzene.SelfHost;
using Microsoft.Extensions.Hosting;

namespace Benzene.HostedService;

public class BenzeneHostedServiceAdapter : IHostedService
{
    private readonly IBenzeneWorker _benzeneWorker;

    public BenzeneHostedServiceAdapter(IBenzeneWorker benzeneWorker)
    {
        _benzeneWorker = benzeneWorker;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _benzeneWorker.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _benzeneWorker.StopAsync(cancellationToken);
    }
}

public static class BenzeneWorkerExtensions
{
    public static BenzeneHostedServiceAdapter BuildHostedService(this IBenzeneWorkerBuilder source)
    {
        return new BenzeneHostedServiceAdapter(source.Build());
    }
}




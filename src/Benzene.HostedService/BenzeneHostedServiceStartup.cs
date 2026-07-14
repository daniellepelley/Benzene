using Benzene.Abstractions.Hosting;
using Benzene.SelfHost;
using Microsoft.Extensions.Hosting;

namespace Benzene.HostedService;

[System.Obsolete("Superseded by the platform-neutral BenzeneStartUp hosted via IHostBuilder.UseBenzene<TStartUp>(), whose Configure takes IBenzeneApplicationBuilder. See docs/migration-alpha-to-1.0.md.")]
public abstract class BenzeneHostedServiceStartup : BenzeneWorkerStartup, IHostedService;


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




using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost;

public class WorkerApplicationBuilder : BenzeneApplicationBuilder
{
    public const string PlatformName = "Worker";
    private readonly BenzeneWorkerBuilder _workerStartup;

    public WorkerApplicationBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
        _workerStartup = new BenzeneWorkerBuilder(benzeneServiceContainer);
    }

    public IBenzeneWorkerStartup Workers => _workerStartup;

    public IBenzeneWorker CreateWorker(IServiceResolverFactory serviceResolverFactory) =>
        _workerStartup.Create(serviceResolverFactory);
}

public static class WorkerApplicationBuilderExtensions
{
    /// <summary>Applies worker-host-specific configuration. No-op on other platforms.</summary>
    public static IBenzeneApplicationBuilder UseWorker(this IBenzeneApplicationBuilder app,
        Action<IBenzeneWorkerStartup> configure)
    {
        if (app is WorkerApplicationBuilder worker)
        {
            configure(worker.Workers);
        }
        return app;
    }
}

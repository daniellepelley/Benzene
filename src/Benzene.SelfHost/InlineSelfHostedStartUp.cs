using Benzene.Abstractions.Hosting;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.SelfHost;

public interface IBenzeneWorkerBuilder
{
    public IBenzeneWorker Build();
}

public class InlineSelfHostedStartUp : IBenzeneWorkerBuilder
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IBenzeneWorkerStartup> _appAction = _ => { };

    public InlineSelfHostedStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }

    public InlineSelfHostedStartUp Configure(Action<IBenzeneWorkerStartup> action)
    {
        _appAction = action;
        return this;
    }

    public IBenzeneWorker Build()
    {
        var services = new ServiceCollection();
        var app = new BenzeneWorkerStartup2(new MicrosoftBenzeneServiceContainer(services));

        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return app.Create(serviceResolverFactory);
    }
}

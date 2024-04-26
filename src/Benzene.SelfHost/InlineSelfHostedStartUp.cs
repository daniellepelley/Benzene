using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.SelfHost;

public class InlineSelfHostedStartUp
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IBenzeneWorkerBuilder> _appAction = _ => { };

    public InlineSelfHostedStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }

    public InlineSelfHostedStartUp Configure(Action<IBenzeneWorkerBuilder> action)
    {
        _appAction = action;
        return this;
    }

    public IBenzeneWorker Build()
    {
        var services = new ServiceCollection();
        var app = new BenzeneWorkerBuilder(new MicrosoftBenzeneServiceContainer(services));

        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return app.Create(serviceResolverFactory);
    }
}

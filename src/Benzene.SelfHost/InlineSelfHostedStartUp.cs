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
        var app = new BenzeneWorkerBuilder(new MicrosoftBenzeneServiceContainer(services));

        // ConfigureServices runs before Configure, matching every other host
        // (HostBuilderExtensions.UseBenzene, the Google Cloud function hosts, ...). Because most
        // message-handler registrations are TryAdd (first-registration-wins), a service the caller
        // registers in ConfigureServices (e.g. a custom ISerializer before AddBenzene) must land
        // first so it wins over whatever the Configure/UseMessageHandlers path TryAdds.
        _servicesAction(services);
        _appAction(app);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return app.Create(serviceResolverFactory);
    }
}

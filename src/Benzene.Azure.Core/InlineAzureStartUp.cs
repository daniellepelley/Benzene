using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Azure.Core;

public class InlineAzureStartUp
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IAzureAppBuilder> _appAction = _ => { };


    public InlineAzureStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public InlineAzureStartUp Configure(Action<IAzureAppBuilder> action)
    {
        _appAction = action;
        return this;
    }

    public IAzureApp Build()
    {
        var services = new ServiceCollection();
        var app = new AzureAppBuilder(new MicrosoftBenzeneServiceContainer(services));
        
        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return app.Create(serviceResolverFactory);
    }
}

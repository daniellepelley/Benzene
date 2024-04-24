using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Azure.Core;

public class InlineAzureFunctionStartUp
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IAzureFunctionAppBuilder> _appAction = _ => { };


    public InlineAzureFunctionStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public InlineAzureFunctionStartUp Configure(Action<IAzureFunctionAppBuilder> action)
    {
        _appAction = action;
        return this;
    }

    public IAzureFunctionApp Build()
    {
        var services = new ServiceCollection();
        var app = new AzureFunctionFunctionAppBuilder(new MicrosoftBenzeneServiceContainer(services));
        
        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return app.Create(serviceResolverFactory);
    }
}

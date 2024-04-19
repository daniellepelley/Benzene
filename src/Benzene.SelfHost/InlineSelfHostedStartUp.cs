using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.SelfHost;

public class InlineSelfHostedStartUp
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IMiddlewarePipelineBuilder<SelfHostContext>> _appAction = _ => { };

    public InlineSelfHostedStartUp ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }

    public InlineSelfHostedStartUp Configure(Action<IMiddlewarePipelineBuilder<SelfHostContext>> action)
    {
        _appAction = action;
        return this;
    }

    public IEntryPointMiddlewareApplication<string, string> Build()
    {
        var services = new ServiceCollection();
        var app = new MiddlewarePipelineBuilder<SelfHostContext>(new MicrosoftBenzeneServiceContainer(services));

        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return new SelfHostMiddlewareApplication(app.Build(), serviceResolverFactory);
    }
}

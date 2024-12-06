
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Tools;

public class InlineStartUp<TContext>
{
    private Action<IServiceCollection> _servicesAction = _ => { };
    private Action<IMiddlewarePipelineBuilder<TContext>> _appAction = _ => { };

    public InlineStartUp<TContext> ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public InlineStartUp<TContext> Configure(Action<IMiddlewarePipelineBuilder<TContext>> action)
    {
        _appAction = action;
        return this;
    }

    public IEntryPointMiddlewareApplication<TRequest, TResponse> Build<TRequest, TResponse>(
        Func<IMiddlewarePipeline<TContext>, IServiceResolverFactory, IEntryPointMiddlewareApplication<TRequest, TResponse>> builder)
    {
        var services = new ServiceCollection();
        var app = new MiddlewarePipelineBuilder<TContext>(new MicrosoftBenzeneServiceContainer(services));
        
        _appAction(app);
        _servicesAction(services);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        return builder(app.Build(), serviceResolverFactory);
    }
}

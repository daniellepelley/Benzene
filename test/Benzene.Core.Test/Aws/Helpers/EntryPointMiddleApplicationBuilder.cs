using System;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Test.Aws.Helpers;

public class EntryPointMiddleApplicationBuilder<TEvent, TContext, TResult>
{
    private Action<IServiceCollection> _servicesAction;
    private Action<IMiddlewarePipelineBuilder<TContext>> _appAction;

    public EntryPointMiddleApplicationBuilder<TEvent, TContext, TResult> ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public EntryPointMiddleApplicationBuilder<TEvent, TContext, TResult> Configure(Action<IMiddlewarePipelineBuilder<TContext>> action)
    {
        _appAction = action;
        return this;
    }

    public IEntryPointMiddlewareApplication<TEvent, TResult> Build(Func<IMiddlewarePipeline<TContext>, IMiddlewareApplication<TEvent, TResult>> func)
    {
        var services = new ServiceCollection();
        var app = new MiddlewarePipelineBuilder<TContext>(new MicrosoftBenzeneServiceContainer(services));
        
        _servicesAction(services);
        
        _appAction(app);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        return new EntryPointMiddlewareApplication<TEvent, TResult>(func(app.AsPipeline()), serviceResolverFactory);
    }
}

public class EntryPointMiddleApplicationBuilder<TEvent, TContext>
{
    private Action<IServiceCollection> _servicesAction;
    private Action<IMiddlewarePipelineBuilder<TContext>> _appAction;

    public EntryPointMiddleApplicationBuilder<TEvent, TContext> ConfigureServices(Action<IServiceCollection> action)
    {
        _servicesAction = action;
        return this;
    }
    
    public EntryPointMiddleApplicationBuilder<TEvent, TContext> Configure(Action<IMiddlewarePipelineBuilder<TContext>> action)
    {
        _appAction = action;
        return this;
    }

    public IEntryPointMiddlewareApplication<TEvent> Build(Func<IMiddlewarePipeline<TContext>, IMiddlewareApplication<TEvent>> func)
    {
        var services = new ServiceCollection();
        var app = new MiddlewarePipelineBuilder<TContext>(new MicrosoftBenzeneServiceContainer(services));
        
        _servicesAction(services);
        
        _appAction(app);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        return new EntryPointMiddlewareApplication<TEvent>(func(app.AsPipeline()), serviceResolverFactory);
    }
}


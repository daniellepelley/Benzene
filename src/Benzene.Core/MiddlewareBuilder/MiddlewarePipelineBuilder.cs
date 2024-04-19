using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.DI;
using Benzene.Core.Middleware;

namespace Benzene.Core.MiddlewareBuilder;

public class MiddlewarePipelineBuilder<TContext> : IMiddlewarePipelineBuilder<TContext>
{
    private readonly List<Func<IServiceResolver, IMiddleware<TContext>>> _items = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public MiddlewarePipelineBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
        _benzeneServiceContainer.AddBenzene();
    }

    public IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func)
    {
        _items.Add(func);
        return this;
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(_benzeneServiceContainer);
    }

    public Func<IServiceResolver, IMiddleware<TContext>>[] GetItems() => _items.ToArray();
    
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_benzeneServiceContainer);
    }

    public IMiddlewarePipeline<TContext> Build()
    {
        return new MiddlewarePipeline<TContext>(GetItems());
    }
}

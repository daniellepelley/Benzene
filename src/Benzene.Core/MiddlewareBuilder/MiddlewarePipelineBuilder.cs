using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.DI;

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

    public void Add(Func<IServiceResolver, IMiddleware<TContext>> func)
    {
        _items.Add(func);
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
}

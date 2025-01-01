using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewarePipelineBuilder<TContext> : IMiddlewarePipelineBuilder<TContext>
{
    private readonly List<Func<IServiceResolver, IMiddleware<TContext>>> _items = new();
    private readonly IRegisterDependency _registerDependency;

    public MiddlewarePipelineBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        :this(new RegisterDependency(benzeneServiceContainer))
    { }

    public MiddlewarePipelineBuilder(IRegisterDependency registerDependency)
    {
        _registerDependency = registerDependency;
        registerDependency.Register(x => x.AddBenzeneMiddleware());
    }

    public IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func)
    {
        _items.Add(func);
        return this;
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
       _registerDependency.Register(action); 
    }

    public Func<IServiceResolver, IMiddleware<TContext>>[] GetItems() => _items.ToArray();
    
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_registerDependency);
    }

    public IMiddlewarePipeline<TContext> Build()
    {
        return new MiddlewarePipeline<TContext>(GetItems());
    }
}
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost;

public class BenzeneWorkerBuilder : IBenzeneWorkerBuilder
{
    private readonly List<Func<IServiceResolverFactory, IBenzeneWorker>> _apps = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public BenzeneWorkerBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    public void Add(Func<IServiceResolverFactory, IBenzeneWorker> func)
    {
        _apps.Add(func);
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(_benzeneServiceContainer);
    }
    
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_benzeneServiceContainer);
    }
    
    public IBenzeneWorker Create(IServiceResolverFactory serviceResolverFactory)
    {
        return new CompositeBenzeneWorker(_apps.Select(x => x(serviceResolverFactory)));
    }
}

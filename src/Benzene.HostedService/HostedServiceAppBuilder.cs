using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.HostedService;

public class HostedServiceAppBuilder : IHostedServiceAppBuilder
{
    private readonly List<Func<IServiceResolverFactory, IBenzeneConsumer>> _apps = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public HostedServiceAppBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    public void Add(Func<IServiceResolverFactory, IBenzeneConsumer> func)
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
    
    public IBenzeneConsumer Create(IServiceResolverFactory serviceResolverFactory)
    {
        return new CompositeBenzeneConsumer(_apps.Select(x => x(serviceResolverFactory)));
    }
}

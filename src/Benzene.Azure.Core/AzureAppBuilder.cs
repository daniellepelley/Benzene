using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Azure.Core;

public class AzureAppBuilder : IAzureAppBuilder
{
    private readonly List<Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>> _apps = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public AzureAppBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func)
    {
        _apps.Add(func);
    }

    public IAzureApp Create(IServiceResolverFactory serviceResolverFactory)
    {
        return new AzureApp(_apps.ToArray(), serviceResolverFactory);
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
        action(_benzeneServiceContainer);
    }
    
    public IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>()
    {
        return new MiddlewarePipelineBuilder<TNewContext>(_benzeneServiceContainer);
    }
}

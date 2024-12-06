using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Core;

public class AzureFunctionAppBuilder : IAzureFunctionAppBuilder
{
    private readonly List<Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>> _apps = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public AzureFunctionAppBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    public void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func)
    {
        _apps.Add(func);
    }

    public IAzureFunctionApp Create(IServiceResolverFactory serviceResolverFactory)
    {
        return new AzureFunctionApp(_apps.ToArray(), serviceResolverFactory);
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

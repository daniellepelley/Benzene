using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;

namespace Benzene.Azure.Core;

public class AzureFunctionFunctionAppBuilder : IAzureFunctionAppBuilder
{
    private readonly List<Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>> _apps = new();
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public AzureFunctionFunctionAppBuilder(IBenzeneServiceContainer benzeneServiceContainer)
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

using Benzene.Abstractions.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public class MicrosoftDependencyInjectionAdapter : IDependencyInjectionAdapter<IServiceCollection>
{
    public IServiceCollection CreateContainer()
    {
        return new ServiceCollection();
    }

    public IBenzeneServiceContainer CreateBenzeneServiceContainer(IServiceCollection container)
    {
        return new MicrosoftBenzeneServiceContainer(container);
    }

    public IServiceResolverFactory CreateBenzeneServiceResolverFactory(IServiceCollection container)
    {
        return new MicrosoftServiceResolverFactory(container);
    }
}
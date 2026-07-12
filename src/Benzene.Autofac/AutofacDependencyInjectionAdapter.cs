using Autofac;
using Benzene.Abstractions.DI;

namespace Benzene.Autofac;

public class AutofacDependencyInjectionAdapter : IDependencyInjectionAdapter<ContainerBuilder>
{
    public ContainerBuilder CreateContainer()
    {
        return new ContainerBuilder();
    }

    public IBenzeneServiceContainer CreateBenzeneServiceContainer(ContainerBuilder container)
    {
        return new AutofacBenzeneServiceContainer(container);
    }

    public IServiceResolverFactory CreateBenzeneServiceResolverFactory(ContainerBuilder container)
    {
        return new AutofacServiceResolverFactory(container);
    }
}
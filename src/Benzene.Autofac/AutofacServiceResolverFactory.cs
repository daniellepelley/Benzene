using Autofac;
using Benzene.Abstractions.DI;

namespace Benzene.Autofac;

public class AutofacServiceResolverFactory : IServiceResolverFactory
{
    private readonly IContainer _container;

    public AutofacServiceResolverFactory(ContainerBuilder containerBuilder)
    {
        _container = containerBuilder.Build();
    }

    public void Dispose()
    {
    }

    public IServiceResolver CreateScope()
    {
        return new AutofacServiceResolverAdapter(_container.BeginLifetimeScope(), this);
    }
}
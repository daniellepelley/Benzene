using Autofac;
using Benzene.Abstractions.DI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.Autofac;

public class AutofacServiceResolverFactory : IServiceResolverFactory
{
    private readonly IContainer _container;

    public AutofacServiceResolverFactory(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterInstance(NullLoggerFactory.Instance).As<ILoggerFactory>()
            .IfNotRegistered(typeof(ILoggerFactory));
        containerBuilder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance()
            .IfNotRegistered(typeof(ILogger<>));
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
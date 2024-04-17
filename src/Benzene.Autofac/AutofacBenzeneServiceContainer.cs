using Autofac;
using Autofac.Core;
using Benzene.Abstractions.DI;

namespace Benzene.Autofac;

public class AutofacBenzeneServiceContainer : IBenzeneServiceContainer
{
    private readonly ContainerBuilder _containerBuilder;

    public AutofacBenzeneServiceContainer(ContainerBuilder containerBuilder)
    {
        _containerBuilder = containerBuilder;
    }

    public bool IsTypeRegistered<TService>()
    {
        return IsTypeRegistered(typeof(TService));
    }

    public bool IsTypeRegistered(Type type)
    {
        return _containerBuilder.ComponentRegistryBuilder.IsRegistered(new TypedService(type));
    }

    public IBenzeneServiceContainer AddScoped(Type type)
    {
        if (type.IsGenericType)
        {
            _containerBuilder.RegisterGeneric(type).InstancePerLifetimeScope();
        }
        else
        {
            _containerBuilder.RegisterType(type).InstancePerLifetimeScope();
        }

        return this;
    }

    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        if (implementationType.IsGenericType)
        {
            _containerBuilder.RegisterGeneric(implementationType).As(serviceType).InstancePerLifetimeScope();
        }
        else
        {
            _containerBuilder.RegisterGeneric(implementationType).As(serviceType).InstancePerLifetimeScope();
        }

        return this;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>() where TImplementation : class
    {
        _containerBuilder.RegisterType<TImplementation>().InstancePerLifetimeScope();
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TService, TImplementation>()
        where TService : class where TImplementation : class, TService
    {
        _containerBuilder.RegisterType<TImplementation>().As<TService>().InstancePerLifetimeScope();
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        _containerBuilder
            .Register<TImplementation>(x => func(new AutofacServiceResolverAdapter(x.Resolve<IComponentContext>())))
            .InstancePerLifetimeScope();
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>() where TImplementation : class
    {
        _containerBuilder.RegisterType<TImplementation>().SingleInstance();
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _containerBuilder.RegisterType<TImplementation>().As<TService>().SingleInstance();
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        if (type.IsGenericType)
        {
            _containerBuilder.RegisterGeneric(type).SingleInstance();
        }
        else
        {
            _containerBuilder.RegisterType(type).SingleInstance();
        }

        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        if (implementationType.IsGenericType)
        {
            _containerBuilder.RegisterGeneric(implementationType).As(serviceType).SingleInstance();
        }
        else
        {
            _containerBuilder.RegisterType(implementationType).As(serviceType).SingleInstance();
        }

        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        _containerBuilder
            .Register<TImplementation>(x => func(new AutofacServiceResolverAdapter(x.Resolve<IComponentContext>())))
            .SingleInstance();
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation)
        where TImplementation : class
    {
        _containerBuilder.RegisterInstance(implementation);
        return this;
    }

    public IBenzeneServiceContainer AddServiceResolver()
    {
        _containerBuilder
            .Register<IServiceResolver>(x => new AutofacServiceResolverAdapter(x.Resolve<IComponentContext>()))
            .InstancePerLifetimeScope();
        return this;
    }
}
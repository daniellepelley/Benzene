using Benzene.Abstractions.DI;
using DryIoc;

namespace Benzene.DryIoc;

/// <summary>
/// Adapts Benzene's <see cref="IBenzeneServiceContainer"/> onto a DryIoc <see cref="IContainer"/>.
/// Benzene lifetimes map to DryIoc <see cref="Reuse"/>: <c>AddSingleton</c> → <see cref="Reuse.Singleton"/>,
/// <c>AddScoped</c> → <see cref="Reuse.Scoped"/>, <c>AddTransient</c> → <see cref="Reuse.Transient"/>.
/// Open generics need no special handling here — DryIoc's <c>Register(Type, Type)</c> accepts an open
/// generic implementation directly (unlike Autofac's separate <c>RegisterGeneric</c>).
/// </summary>
public class DryIocBenzeneServiceContainer : IBenzeneServiceContainer
{
    private readonly IContainer _container;

    public DryIocBenzeneServiceContainer(IContainer container)
    {
        _container = container;
    }

    public bool IsTypeRegistered<TService>()
    {
        return IsTypeRegistered(typeof(TService));
    }

    public bool IsTypeRegistered(Type type)
    {
        return _container.IsRegistered(type);
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>() where TImplementation : class
    {
        _container.Register<TImplementation>(Reuse.Scoped);
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TService, TImplementation>()
        where TService : class where TImplementation : class, TService
    {
        _container.Register<TService, TImplementation>(Reuse.Scoped);
        return this;
    }

    public IBenzeneServiceContainer AddScoped(Type type)
    {
        _container.Register(type, Reuse.Scoped);
        return this;
    }

    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        _container.Register(serviceType, implementationType, Reuse.Scoped);
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        // Register the supplied instance (not a freshly-constructed TImplementation) with scoped reuse,
        // to honour the "using an existing instance" contract and match the Autofac/Microsoft adapters.
        _container.RegisterDelegate(_ => implementation, Reuse.Scoped);
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        _container.RegisterDelegate(r => func(new DryIocServiceResolverAdapter(r)), Reuse.Scoped);
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>() where TImplementation : class
    {
        _container.Register<TImplementation>(Reuse.Transient);
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TService, TImplementation>()
        where TService : class where TImplementation : class, TService
    {
        _container.Register<TService, TImplementation>(Reuse.Transient);
        return this;
    }

    public IBenzeneServiceContainer AddTransient(Type type)
    {
        _container.Register(type, Reuse.Transient);
        return this;
    }

    public IBenzeneServiceContainer AddTransient(Type serviceType, Type implementationType)
    {
        _container.Register(serviceType, implementationType, Reuse.Transient);
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _container.RegisterDelegate(_ => implementation, Reuse.Transient);
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        _container.RegisterDelegate(r => func(new DryIocServiceResolverAdapter(r)), Reuse.Transient);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>() where TImplementation : class
    {
        _container.Register<TImplementation>(Reuse.Singleton);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>()
        where TService : class where TImplementation : class, TService
    {
        _container.Register<TService, TImplementation>(Reuse.Singleton);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        _container.Register(type, Reuse.Singleton);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        _container.Register(serviceType, implementationType, Reuse.Singleton);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation)
        where TImplementation : class
    {
        _container.RegisterInstance(implementation);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        _container.RegisterDelegate(r => func(new DryIocServiceResolverAdapter(r)), Reuse.Singleton);
        return this;
    }

    public IServiceResolverFactory CreateServiceResolverFactory()
    {
        return new DryIocServiceResolverFactory(_container);
    }

    public IBenzeneServiceContainer AddServiceResolver()
    {
        _container.RegisterDelegate<IServiceResolver>(r => new DryIocServiceResolverAdapter(r), Reuse.Scoped);
        return this;
    }
}

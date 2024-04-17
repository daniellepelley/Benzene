using Benzene.Abstractions.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public class MicrosoftBenzeneServiceContainer : IBenzeneServiceContainer
{
    private readonly IServiceCollection _services;

    public MicrosoftBenzeneServiceContainer(IServiceCollection services)
    {
        _services = services;
    }

    public bool IsTypeRegistered<TService>()
    {
        return IsTypeRegistered(typeof(TService));
    }

    public bool IsTypeRegistered(Type type)
    {
        return _services.Any(x => x.ServiceType == type);
    }

    public IBenzeneServiceContainer AddScoped(Type type)
    {
        _services.AddScoped(type);
        return this;
    }

    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        _services.AddScoped(serviceType, implementationType);
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TService>() where TService : class
    {
        _services.AddScoped<TService>();
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _services.AddScoped<TService, TImplementation>();
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TService>(Func<IServiceResolver, TService> func) where TService : class
    {
        _services.AddScoped<TService>(x => func(new MicrosoftServiceResolverAdapter(x)));
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService>() where TService : class
    {
        _services.AddSingleton<TService>();
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService, TImplementation>();
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        _services.AddSingleton(type);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        _services.AddSingleton(serviceType, implementationType);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService>(Func<IServiceResolver, TService> func) where TService : class
    {
        _services.AddSingleton<TService>(x => func(new MicrosoftServiceResolverAdapter(x)));
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService>(TService implementation) where TService : class
    {
        _services.AddSingleton(implementation);
        return this;
    }
    
    public IBenzeneServiceContainer AddServiceResolver()
    {
        _services.AddTransient<IServiceResolver>(x => new MicrosoftServiceResolverAdapter(x));
        return this;
    }
}
